using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>Chama as ferramentas como o modelo chamaria: as de leitura só com o ano e o mês, e as do vínculo com os ids.</summary>
internal static class ClienteMcpExtensions
{
    extension(McpClient clienteMcp)
    {
        public ValueTask<CallToolResult> ChamarDoMesAsync(string ferramenta, int ano, int mes)
        {
            return clienteMcp.CallToolAsync(ferramenta, new Dictionary<string, object?> { ["ano"] = ano, ["mes"] = mes });
        }

        public async Task<McpClientTool> ObterFerramentaAsync(string nome)
        {
            IList<McpClientTool> ferramentas = await clienteMcp.ListToolsAsync();
            return ferramentas.Single(f => f.Name == nome);
        }

        public ValueTask<CallToolResult> PreverVinculoAsync(Guid idTransacao, Guid idDespesa)
        {
            return clienteMcp.CallToolAsync(
                "prever_vinculo",
                new Dictionary<string, object?> { ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada.</summary>
        public ValueTask<CallToolResult> ConfirmarVinculoAsync(Guid idTransacao, Guid idDespesa, string resumo)
        {
            return clienteMcp.CallToolAsync(
                "confirmar_vinculo",
                new Dictionary<string, object?> { ["resumo"] = resumo, ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }
    }

    extension(CallToolResult resultado)
    {
        /// <summary>O texto que o modelo recebe, exatamente como o servidor o escreveu.</summary>
        public string Texto => resultado.Content.OfType<TextContentBlock>().Single().Text;

        public JsonObject Json => JsonNode.Parse(resultado.Texto)!.AsObject();
    }
}
