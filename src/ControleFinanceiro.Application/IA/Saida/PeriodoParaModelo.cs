using ControleFinanceiro.Domain.Importacoes;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>Um período de datas, com o primeiro e o último dia.</summary>
public sealed record PeriodoParaModelo : ISaidaModelo
{
    [JsonPropertyName("inicio")]
    public DateOnly Inicio { get; }

    [JsonPropertyName("fim")]
    public DateOnly Fim { get; }

    private PeriodoParaModelo(DateOnly inicio, DateOnly fim)
    {
        Inicio = inicio;
        Fim = fim;
    }

    public static PeriodoParaModelo De(PeriodoArquivo periodo) => new(periodo.Inicio, periodo.Fim);
}
