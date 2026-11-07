using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>Um beneficiário como o modelo o recebe.</summary>
public sealed class BeneficiarioLido
{
    public Guid Id { get; }
    public string Nome { get; }
    public IReadOnlyList<string> Aliases { get; }

    public BeneficiarioLido(JsonObject json)
    {
        Id = json["id"]!.GetValue<Guid>();
        Nome = json.Texto("nome");
        Aliases = json["aliases"]!
            .AsArray()
            .Select(alias => alias!.GetValue<string>())
            .ToArray();
    }
}
