using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>O que o modelo vê de uma transação. O <see cref="Id"/> é gerado por nós, e não vem do banco emissor.</summary>
public sealed record TransacaoParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("data")]
    public DateOnly Data { get; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; }

    /// <summary>Vazia quando só havia documento nela.</summary>
    [JsonPropertyName("descricao")]
    public string Descricao { get; }

    /// <summary>Omitida quando não há ou quando só havia documento nela. Nos Pix do C6, é ela que diz quem recebeu.</summary>
    [JsonPropertyName("contraparte")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Contraparte { get; }

    private TransacaoParaModelo(IdTransacao id, DateOnly data, Reais valor, DocumentoMascarado descricao, DocumentoMascarado contraparte)
    {
        Id = id.Value;
        Data = data;
        Valor = valor;
        Descricao = descricao.ConteudoOuVazio;
        Contraparte = contraparte.TemConteudo ? contraparte.Value : null;
    }

    public static TransacaoParaModelo De(Transacao transacao)
    {
        return new TransacaoParaModelo(
            transacao.Id,
            transacao.Data,
            transacao.Valor,
            (DocumentoMascarado)transacao.Descricao,
            (DocumentoMascarado)transacao.Contraparte);
    }
}
