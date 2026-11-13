using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>As despesas fixas de um mês, ou só o erro que impediu de listá-las.</summary>
public sealed record DespesasFixasDoMesParaModelo : ISaidaModelo
{
    /// <summary>O mês, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    public string? Mes { get; }

    [JsonPropertyName("despesasFixas")]
    public IReadOnlyList<DespesaFixaParaModelo>? DespesasFixas { get; }

    [JsonPropertyName("erro")]
    public ErroParaModelo? Erro { get; }

    private DespesasFixasDoMesParaModelo(MesReferencia mes, IReadOnlyList<DespesaFixaParaModelo> despesasFixas)
    {
        Mes = mes.ToString();
        DespesasFixas = despesasFixas;
    }

    private DespesasFixasDoMesParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    public static DespesasFixasDoMesParaModelo De(MesReferencia mes, IReadOnlyList<DespesaFixaParaModelo> despesasFixas) => new(mes, despesasFixas);

    public static DespesasFixasDoMesParaModelo Falha(ErroParaModelo erro) => new(erro);
}
