using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>A resposta do <c>listar_cartoes</c>.</summary>
public sealed class CartoesLidos
{
    public IReadOnlyList<CartaoLido> Cartoes { get; }

    public CartoesLidos(JsonObject json)
    {
        Cartoes = json["cartoes"]!
            .AsArray()
            .Select(cartao => new CartaoLido(cartao!.AsObject()))
            .ToArray();
    }
}
