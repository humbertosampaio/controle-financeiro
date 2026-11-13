using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// Uma falha prevista, com mensagem fixa que diz ao modelo o que corrigir. Nunca repete a entrada nem o erro de quem
/// falhou. As mensagens ficam no <see cref="ErrosParaModelo"/>, agrupadas por contexto.
/// </summary>
public sealed record ErroParaModelo : ISaidaModelo
{
    [JsonPropertyName("mensagem")]
    public string Mensagem { get; }

    internal ErroParaModelo(DocumentoMascarado mensagem)
    {
        Mensagem = mensagem;
    }
}
