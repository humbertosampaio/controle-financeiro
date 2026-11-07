using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>Uma falha prevista como o modelo a recebe: só a mensagem.</summary>
public sealed class ErroLido
{
    public string Mensagem { get; }

    public ErroLido(JsonObject json)
    {
        Mensagem = json.Texto("mensagem");
    }

    /// <summary>O erro de uma resposta que falhou, no campo <c>erro</c>.</summary>
    public static ErroLido DoEnvelope(JsonObject json) => new(json["erro"]!.AsObject());
}
