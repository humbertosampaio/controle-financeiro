using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>Os candidatos a pagamento das faturas que vencem num mês, ou só o erro que impediu de listá-los.</summary>
public sealed record CandidatosPagamentoFaturaParaModelo : ISaidaModelo
{
    /// <summary>O mês de vencimento, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    public string? Mes { get; }

    [JsonPropertyName("candidatos")]
    public IReadOnlyList<CandidatoPagamentoFaturaParaModelo>? Candidatos { get; }

    [JsonPropertyName("erro")]
    public ErroParaModelo? Erro { get; }

    private CandidatosPagamentoFaturaParaModelo(MesReferencia mes, IReadOnlyList<CandidatoPagamentoFaturaParaModelo> candidatos)
    {
        Mes = mes.ToString();
        Candidatos = candidatos;
    }

    private CandidatosPagamentoFaturaParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    public static CandidatosPagamentoFaturaParaModelo De(MesReferencia mes, IReadOnlyList<CandidatoPagamentoFaturaParaModelo> candidatos) => new(mes, candidatos);

    public static CandidatosPagamentoFaturaParaModelo Falha(ErroParaModelo erro) => new(erro);
}
