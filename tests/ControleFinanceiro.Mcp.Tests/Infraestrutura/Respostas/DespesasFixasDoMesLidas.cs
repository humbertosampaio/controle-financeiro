using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;

/// <summary>A resposta do <c>listar_despesas_fixas</c> num mês válido.</summary>
internal sealed class DespesasFixasDoMesLidas
{
    public string Mes { get; }
    public IReadOnlyList<DespesaFixaLida> DespesasFixas { get; }

    public DespesasFixasDoMesLidas(JsonObject json)
    {
        Mes = json.Texto("mes");
        DespesasFixas = json["despesasFixas"]!
            .AsArray()
            .Select(despesaFixa => new DespesaFixaLida(despesaFixa!.AsObject()))
            .ToArray();
    }
}
