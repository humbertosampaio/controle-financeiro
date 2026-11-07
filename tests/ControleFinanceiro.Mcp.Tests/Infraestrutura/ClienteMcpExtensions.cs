using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>Chama as ferramentas como o modelo chamaria: as de leitura só com o ano e o mês.</summary>
internal static class ClienteMcpExtensions
{
    extension(McpClient clienteMcp)
    {
        public ValueTask<CallToolResult> ChamarDoMesAsync(string ferramenta, int ano, int mes)
        {
            return clienteMcp.CallToolAsync(ferramenta, new Dictionary<string, object?> { ["ano"] = ano, ["mes"] = mes });
        }

        public ValueTask<CallToolResult> VincularAsync(Guid idTransacao, Guid idDespesa)
        {
            return clienteMcp.CallToolAsync(
                "vincular_transacao",
                new Dictionary<string, object?> { ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }
    }

    extension(CallToolResult resultado)
    {
        /// <summary>O texto que o modelo recebe, exatamente como o servidor o escreveu.</summary>
        public string Texto => resultado.Content.OfType<TextContentBlock>().Single().Text;

        public JsonObject Json => JsonNode.Parse(resultado.Texto)!.AsObject();
    }
}
