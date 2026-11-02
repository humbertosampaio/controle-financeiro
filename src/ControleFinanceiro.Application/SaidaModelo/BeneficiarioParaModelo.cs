using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.SaidaModelo;

/// <summary>O que o modelo vê de um beneficiário: quem ele é e como aparece nos arquivos.</summary>
public sealed record BeneficiarioParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    /// <summary>Vazio quando só havia documento nele.</summary>
    [JsonPropertyName("nome")]
    public string Nome { get; }

    /// <summary>Sem os que só tinham documento e sem repetir os que ficaram iguais depois da máscara.</summary>
    [JsonPropertyName("aliases")]
    public IReadOnlyList<string> Aliases { get; }

    private BeneficiarioParaModelo(IdBeneficiario id, DocumentoMascarado nome, IReadOnlyList<DocumentoMascarado> aliases)
    {
        Id = id.Value;
        Nome = nome.ConteudoOuVazio;
        Aliases = aliases
            .Where(alias => alias.TemConteudo)
            .Select(alias => alias.Value)
            .Distinct()
            .ToArray();
    }

    public static BeneficiarioParaModelo De(Beneficiario beneficiario)
    {
        return new BeneficiarioParaModelo(
            beneficiario.Id,
            (DocumentoMascarado)beneficiario.Nome,
            beneficiario.Aliases.Select(alias => (DocumentoMascarado)alias.Alias.TextoOriginal).ToArray());
    }
}
