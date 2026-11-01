using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Tests.Services;

/// <summary>Cria as transações pela <see cref="LoteTransacoesFactory"/>, a única entrada, do usuário que o fixture der.</summary>
public sealed class TransacoesDeTeste
{
    private readonly IdUsuario _idUsuario;
    private readonly ContaBancaria _conta;
    private readonly LayoutArquivoImportacao _layout;
    private readonly PeriodoArquivo _periodo;
    private readonly DateTimeOffset _importadaEm;

    public TransacoesDeTeste(IdUsuario idUsuario, ContaBancaria conta, LayoutArquivoImportacao layout, PeriodoArquivo periodo, DateTimeOffset importadaEm)
    {
        _idUsuario = idUsuario;
        _conta = conta;
        _layout = layout;
        _periodo = periodo;
        _importadaEm = importadaEm;
    }

    public Transacao DaConta(Lancamento lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(_idUsuario, [lancamento], TipoOrigem.Conta);
        ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(_conta, _layout, _periodo, lidas: 1, _importadaEm);
        return LoteTransacoesFactory.Criar(_idUsuario, beneficiarios, categoriasDoUsuario: [], _conta, importacao, [lancamento]).Value.Single();
    }

    public Transacao DoCartao(Cartao cartao, Lancamento lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(_idUsuario, [lancamento], TipoOrigem.Cartao);
        ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(cartao, _layout, _periodo, lidas: 1, _importadaEm);
        return LoteTransacoesFactory.Criar(_idUsuario, beneficiarios, categoriasDoUsuario: [], cartao, importacao, [lancamento]).Value.Single();
    }
}
