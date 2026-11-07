using AutoFixture;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Monta no banco o cenário das ferramentas do vínculo, do usuário do servidor: a despesa é Pix, paga por uma saída da
/// conta. Gravado ou não, o que vale é o que se lê de volta do banco.
/// </summary>
internal sealed class VinculoDeTeste
{
    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public VinculoDeTeste(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
    }

    /// <summary>Uma despesa Pix do usuário do servidor, pendente no mês, e uma saída da conta dele que pode pagá-la.</summary>
    public async Task<(Transacao Transacao, Despesa Despesa)> GravarSaidaEDespesaAsync(IFixture fixture, MesReferencia mes)
    {
        Despesa despesa = await GravarDespesaAsync(fixture, mes);
        Transacao saida = await GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria().ComoSaida());
        return (saida, despesa);
    }

    /// <summary>Uma despesa Pix do usuário do servidor, pendente no mês.</summary>
    public async Task<Despesa> GravarDespesaAsync(IFixture fixture, MesReferencia mes)
    {
        fixture.Inject(_servidor.Usuario.Id);
        return await _banco.GravarDespesaDoMesAsync(await _banco.GravarRecorrenteAsync(fixture), mes);
    }

    /// <summary>Uma despesa pendente no mês de outro usuário, que o servidor não enxerga.</summary>
    public async Task<Despesa> GravarDespesaDeOutroUsuarioAsync(IFixture fixture, Usuario outroUsuario, MesReferencia mes)
    {
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        return await _banco.GravarDespesaDoMesAsync(await _banco.GravarRecorrenteAsync(fixture), mes);
    }

    /// <summary>Grava o lançamento como um extrato de uma conta nova do usuário injetado no <paramref name="fixture"/>.</summary>
    public async Task<Transacao> GravarExtratoAsync(IFixture fixture, Lancamento lancamento) =>
        (await GravarExtratoAsync(fixture, [lancamento])).Single();

    /// <summary>
    /// Grava os lançamentos como um extrato de uma conta nova do usuário injetado no <paramref name="fixture"/>. Uma vez
    /// por teste: a conta fica congelada no fixture.
    /// </summary>
    public async Task<IReadOnlyList<Transacao>> GravarExtratoAsync(IFixture fixture, IReadOnlyList<Lancamento> lancamentos)
    {
        ContaBancaria conta = fixture.Freeze<ContaBancaria>();

        // O Postgres só grava o instante em UTC, como o TimeProvider da aplicação o dá.
        fixture.Inject(fixture.Create<DateTimeOffset>().ToUniversalTime());

        return await _banco.GravarExtratoAsync(conta, fixture.Create<ImportacaoArquivo>(), lancamentos);
    }

    public async Task DeveEstarPendenteAsync(Despesa despesa, MesReferencia mes)
    {
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentoDoMes(mes).Value.Status.Should().Be(StatusPagamento.Pendente);
    }
}
