using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.SaidaModelo;

/// <summary>O que o modelo vê de uma despesa, de qualquer tipo: o alvo de um vínculo.</summary>
public sealed record DespesaParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    /// <summary>Vazia quando só havia documento nela.</summary>
    [JsonPropertyName("descricao")]
    public string Descricao { get; }

    [JsonPropertyName("dataOcorrencia")]
    public DateOnly DataOcorrencia { get; }

    private DespesaParaModelo(IdDespesa id, DocumentoMascarado descricao, DateOnly dataOcorrencia)
    {
        Id = id.Value;
        Descricao = descricao.ConteudoOuVazio;
        DataOcorrencia = dataOcorrencia;
    }

    public static DespesaParaModelo De(Despesa despesa) =>
        new(despesa.Id, (DocumentoMascarado)despesa.Descricao, despesa.DataOcorrencia);
}
