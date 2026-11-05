using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.SaidaModelo;

/// <summary>
/// Uma falha prevista, com mensagem fixa que diz ao modelo o que corrigir. Nunca repete a entrada nem o erro de quem
/// falhou.
/// </summary>
public sealed record ErroParaModelo : ISaidaModelo
{
    public static readonly ErroParaModelo MesInvalido = new((DocumentoMascarado)"Mês inválido: o mês vai de 1 a 12, e o ano, de 1 a 9999.");

    public static readonly ErroParaModelo UsuarioNaoEncontrado = new((DocumentoMascarado)"O usuário configurado no servidor não foi encontrado.");

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; }

    private ErroParaModelo(DocumentoMascarado mensagem)
    {
        Mensagem = mensagem;
    }
}
