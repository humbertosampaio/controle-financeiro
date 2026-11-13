using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>Escolhe o leitor da fatura pelo layout, em qualquer rota.</summary>
public sealed class LeituraFaturaService
{
    private readonly IEnumerable<ILeitorFatura> _leitores;

    public LeituraFaturaService(IEnumerable<ILeitorFatura> leitores)
    {
        _leitores = leitores;
    }

    /// <param name="layout">Um layout de fatura.</param>
    public Task<Result<Fatura>> LerAsync(LayoutArquivoImportacao layout, Stream arquivo, CancellationToken ct)
    {
        return _leitores.Single(l => l.Layout == layout).LerAsync(arquivo, ct);
    }
}
