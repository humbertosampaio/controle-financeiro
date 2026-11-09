using AutoFixture;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.Respostas;
using CSharpFunctionalExtensions;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Monta no banco o cenário das ferramentas do pagamento da fatura, do usuário do servidor: um cartão, despesas adiadas
/// cobradas por compras dele e uma saída da conta que paga a fatura. Tudo sai na mesma data, então as compras caem na
/// mesma fatura. Gravado ou não, o que vale é o que se lê de volta do banco.
/// </summary>
internal sealed class PagamentoFaturaDeTeste
{
    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;
    private readonly VinculoDeTeste _vinculo;

    public PagamentoFaturaDeTeste(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
        _vinculo = new VinculoDeTeste(servidor);
    }

    /// <summary>Um cartão do usuário do servidor, uma adiada cobrada por uma compra dele e uma saída da conta.</summary>
    public async Task<FaturaDeTeste> GravarAsync(IFixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        fixture.Freeze<DateOnly>();
        Cartao cartao = fixture.Freeze<Cartao>();

        DespesaAdiada adiada = await GravarAdiadaCobradaAsync(fixture, cartao);
        Transacao saida = await _vinculo.GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria().ComoSaida());

        return new FaturaDeTeste(cartao, adiada, saida);
    }

    /// <summary>
    /// Uma compra à vista no cartão, gravada como uma fatura importada, e a adiada que ela cobra, pendente. Chamada depois
    /// do <see cref="GravarAsync"/>, a compra cai na mesma fatura.
    /// </summary>
    public async Task<DespesaAdiada> GravarAdiadaCobradaAsync(IFixture fixture, Cartao cartao)
    {
        // O Postgres só grava o instante em UTC, como o TimeProvider da aplicação o dá.
        ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(
            cartao,
            fixture.Create<LayoutArquivoImportacao>(),
            fixture.Create<PeriodoArquivo>(),
            lidas: 1,
            fixture.Create<DateTimeOffset>().ToUniversalTime());
        Lancamento compra = fixture.Create<Lancamento>().SemCategoria().ComoSaida().NaOrigem(TipoOrigem.Cartao);
        Transacao transacao = (await _banco.GravarFaturaAsync(cartao, importacao, [compra])).Single();

        TextoObrigatorio descricao = fixture.Create<TextoObrigatorio>();
        Beneficiario beneficiario = BeneficiarioFactory
            .Resolver(cartao.IdUsuario, beneficiariosDoUsuario: [], [new BeneficiarioNovo(descricao, alias: null)])
            .Value[0];
        DespesaAdiada adiada = DespesaAdiada.Criar(
            cartao.IdUsuario,
            beneficiario.Id,
            descricao,
            idCategoria: null,
            transacao.Data,
            fixture.Create<ReaisPositivo>(),
            cartao.Id,
            cartao.VencimentoDaCompra(transacao.Data),
            idDespesaRecorrente: null);
        VinculoTransacaoService.Vincular(transacao, adiada, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();

        await _banco.GravarDespesaAsync(adiada, beneficiario);
        return adiada;
    }

    /// <summary>Paga a fatura pelas duas etapas, como outra conversa faria antes da chamada do teste.</summary>
    public async Task PagarAsync(Transacao saida, Cartao cartao, MesReferencia mesVencimento)
    {
        CallToolResult previa = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, mesVencimento);
        string resumo = new PagamentoFaturaLido(previa.Json).Resumo;

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, mesVencimento, resumo);
        resultado.Json.Should().NotContainKey("erro");
    }

    public async Task DeveEstarPendenteAsync(DespesaAdiada despesa)
    {
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentoDoMes(despesa.MesReferencia).Value.Status.Should().Be(StatusPagamento.Pendente);
    }

    public async Task DeveEstarPagaPelaSaidaAsync(Despesa despesa, Transacao saida)
    {
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentosRealizadosPor(saida.Id).Should().ContainSingle().Which.Status.Should().Be(StatusPagamento.Pago);
    }
}

/// <summary>O que o <see cref="PagamentoFaturaDeTeste.GravarAsync"/> gravou.</summary>
/// <param name="Adiada">A despesa cobrada e pendente, no mês de vencimento da fatura.</param>
/// <param name="Saida">A saída da conta que pode pagar a fatura.</param>
internal sealed record FaturaDeTeste(Cartao Cartao, DespesaAdiada Adiada, Transacao Saida);
