using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>
/// As importações e as transações de um usuário, montadas pelas factories, e o <see cref="CandidatosPagamentoFaturaService"/>
/// sobre elas. Cada fatura importada é um arquivo próprio. Com <c>[Frozen] IdUsuario</c> no teste, tudo sai desse usuário.
/// </summary>
public sealed class CenarioPagamentoFatura
{
    private readonly IdUsuario _idUsuario;
    private readonly ContaBancaria _conta;
    private readonly LayoutArquivoImportacao _layout;
    private readonly Periodo _periodo;
    private readonly DateTimeOffset _importadaEm;
    private readonly List<ImportacaoFatura> _importacoes = [];
    private readonly List<Transacao> _transacoes = [];

    public CenarioPagamentoFatura(IdUsuario idUsuario, ContaBancaria conta, LayoutArquivoImportacao layout, Periodo periodo, DateTimeOffset importadaEm)
    {
        _idUsuario = idUsuario;
        _conta = conta;
        _layout = layout;
        _periodo = periodo;
        _importadaEm = importadaEm;
    }

    /// <returns>As transações da fatura, na ordem das linhas.</returns>
    public IReadOnlyList<Transacao> ImportarFatura(Cartao cartao, VencimentoFatura vencimento, params Lancamento[] linhas)
    {
        ImportacaoFatura importacao = ImportacaoArquivoFactory
            .Registrar(cartao, cartoesUsuario: [cartao], importacoesFaturaMes: [], _layout, _periodo, vencimento, linhas.Length, _importadaEm)
            .Value;
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(_idUsuario, linhas);
        IReadOnlyList<Transacao> transacoes = LoteTransacoesCartaoFactory
            .Criar(_idUsuario, beneficiarios, categoriasDoUsuario: [], cartao, importacao, linhas)
            .Value;

        _importacoes.Add(importacao);
        _transacoes.AddRange(transacoes);
        return transacoes;
    }

    /// <summary>Uma saída da conta na data e no valor do caso, com os textos e a identidade da linha gerada.</summary>
    public Transacao Saida(Lancamento linha, DateOnly data, ReaisPositivo valor)
    {
        Lancamento lancamento = linha.NaContaComValor(data, -valor);
        ImportacaoExtrato importacao = ImportacaoArquivoFactory.Registrar(_conta, _layout, _periodo, lidas: 1, _importadaEm);
        Transacao saida = LoteTransacoesContaFactory
            .Criar(_idUsuario, BeneficiariosResolvidos.Dos(_idUsuario, [lancamento]), categoriasDoUsuario: [], _conta, importacao, [lancamento])
            .Value
            .Single();

        _transacoes.Add(saida);
        return saida;
    }

    public IReadOnlyList<CandidatoPagamentoFatura> Apontar(IReadOnlyCollection<Cartao> cartoesUsuario, MesReferencia mesVencimento) =>
        Apontar(cartoesUsuario, mesVencimento, despesasMeses: [], despesasPagasSaidas: []);

    public IReadOnlyList<CandidatoPagamentoFatura> Apontar(
        IReadOnlyCollection<Cartao> cartoesUsuario,
        MesReferencia mesVencimento,
        IReadOnlyCollection<Despesa> despesasMeses,
        IReadOnlyCollection<Despesa> despesasPagasSaidas)
    {
        return CandidatosPagamentoFaturaService.Apontar(
            _idUsuario,
            [mesVencimento],
            cartoesUsuario,
            _importacoes,
            _transacoes,
            despesasMeses,
            despesasPagasSaidas);
    }
}
