using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>As respostas que o usuário do cliente de teste dá à elicitation, programadas no substituto da fixture.</summary>
internal static class RespostaElicitacao
{
    public static ElicitResult Confirmar() => Com("accept", confirmar: true);

    public static ElicitResult Recusar() => Com("decline", confirmar: false);

    /// <param name="acao"><c>accept</c>, <c>decline</c> ou <c>cancel</c>. O campo só vale no aceite.</param>
    public static ElicitResult Com(string acao, bool confirmar)
    {
        return new ElicitResult
        {
            Action = acao,
            Content = new Dictionary<string, JsonElement> { ["confirmar"] = JsonSerializer.SerializeToElement(confirmar) },
        };
    }
}
