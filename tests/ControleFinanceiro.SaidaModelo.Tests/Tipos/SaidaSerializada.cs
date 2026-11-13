using System.Text.Json;
using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

/// <summary>
/// O payload de um tipo de saída, comparado como JSON porque o encoder padrão escapa acentos e <c>·</c>. Serializa com as
/// opções do SDK do MCP, as que levam o retorno das ferramentas ao modelo: elas omitem as propriedades nulas, e por isso
/// os tipos de saída não marcam cada uma.
/// </summary>
internal static class SaidaSerializada
{
    extension(ISaidaModelo saida)
    {
        public JsonObject Serializar()
        {
            return JsonSerializer.SerializeToNode(saida, saida.GetType(), McpJsonUtilities.DefaultOptions)!.AsObject();
        }
    }
}
