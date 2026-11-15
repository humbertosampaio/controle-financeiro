using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Escolhe o leitor da fatura pelo layout, em qualquer rota: o layout cujo arquivo não traz o vencimento é lido com a data
/// que o usuário informou.
/// </summary>
public sealed class LeituraFaturaAppService
{
    private readonly IEnumerable<ILeitorFatura> _leitores;
    private readonly IEnumerable<ILeitorFaturaVencimentoInformado> _leitoresVencimentoInformado;

    public LeituraFaturaAppService(IEnumerable<ILeitorFatura> leitores, IEnumerable<ILeitorFaturaVencimentoInformado> leitoresVencimentoInformado)
    {
        _leitores = leitores;
        _leitoresVencimentoInformado = leitoresVencimentoInformado;
    }

    /// <param name="layout">Um layout de fatura.</param>
    /// <param name="vencimento">Presente quando o layout é de vencimento informado; nos outros, não é lido.</param>
    /// <exception cref="InvalidOperationException">Quando o layout é de vencimento informado e a data não veio.</exception>
    public Task<Result<Fatura, FalhaLeituraArquivo>> LerAsync(
        LayoutArquivoImportacao layout,
        Stream arquivo,
        Maybe<VencimentoFatura> vencimento,
        CancellationToken ct)
    {
        if (!LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
            return _leitores.Single(l => l.Layout == layout).LerAsync(arquivo, ct);

        VencimentoFatura informado = vencimento.GetValueOrThrow($"O layout {layout.Value} pede o vencimento da fatura.");
        return _leitoresVencimentoInformado.Single(l => l.Layout == layout).LerAsync(arquivo, informado, ct);
    }
}
