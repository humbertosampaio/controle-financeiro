using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>A resposta do <c>listar_despesas_fixas</c> num mês válido.</summary>
public sealed class DespesasFixasDoMesLidas
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
