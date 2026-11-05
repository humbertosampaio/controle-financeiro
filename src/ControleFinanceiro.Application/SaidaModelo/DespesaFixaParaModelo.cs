using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.SaidaModelo;

/// <summary>O que o modelo vê de uma despesa recorrente num mês, com a despesa que ela gerou nele, quando já gerou.</summary>
public sealed record DespesaFixaParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    /// <summary>Vazia quando só havia documento nela.</summary>
    [JsonPropertyName("descricao")]
    public string Descricao { get; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; }

    [JsonPropertyName("diaOcorrencia")]
    public int DiaOcorrencia { get; }

    /// <summary>Omitida quando a despesa do mês ainda não foi gerada.</summary>
    [JsonPropertyName("despesaDoMes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DespesaParaModelo? DespesaDoMes { get; }

    private DespesaFixaParaModelo(
        IdDespesaRecorrente id,
        DocumentoMascarado descricao,
        Reais valor,
        DiaOcorrenciaRecorrente diaOcorrencia,
        DespesaParaModelo? despesaDoMes)
    {
        Id = id.Value;
        Descricao = descricao.ConteudoOuVazio;
        Valor = valor;
        DiaOcorrencia = diaOcorrencia;
        DespesaDoMes = despesaDoMes;
    }

    /// <param name="despesaDoMes">A despesa que a recorrente gerou no mês, ou nulo quando ainda não gerou.</param>
    public static DespesaFixaParaModelo De(DespesaRecorrente recorrente, Despesa? despesaDoMes, MesReferencia mes)
    {
        return new DespesaFixaParaModelo(
            recorrente.Id,
            (DocumentoMascarado)recorrente.Descricao,
            recorrente.Valor,
            recorrente.DiaOcorrencia,
            despesaDoMes is null ? null : DespesaParaModelo.De(despesaDoMes, mes));
    }
}
