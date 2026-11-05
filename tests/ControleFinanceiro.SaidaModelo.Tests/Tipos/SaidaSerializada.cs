using System.Text.Json;
using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

/// <summary>O payload de um tipo de saída, comparado como JSON porque o encoder padrão escapa acentos e <c>·</c>.</summary>
internal static class SaidaSerializada
{
    extension(ISaidaModelo saida)
    {
        /// <returns>O JSON do tipo, depois de conferir que as opções padrão e as do SDK do MCP produzem o mesmo.</returns>
        public JsonObject Serializar()
        {
            JsonNode? padrao = JsonSerializer.SerializeToNode(saida, saida.GetType(), JsonSerializerOptions.Default);
            JsonNode? sdk = JsonSerializer.SerializeToNode(saida, saida.GetType(), McpJsonUtilities.DefaultOptions);

            JsonNode.DeepEquals(padrao, sdk).Should().BeTrue();
            return padrao!.AsObject();
        }
    }

    extension(JsonObject json)
    {
        public string Texto(string campo) => json[campo]!.GetValue<string>();
    }
}
