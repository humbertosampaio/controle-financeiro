using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;

/// <summary>A despesa que uma recorrente gerou no mês, como o modelo a recebe.</summary>
internal sealed class DespesaDoMesLida
{
    public Guid Id { get; }
    public string Descricao { get; }
    public DateOnly DataOcorrencia { get; }
    public decimal Valor { get; }
    public string Mes { get; }
    public string Status { get; }

    public DespesaDoMesLida(JsonObject json)
    {
        Id = json["id"]!.GetValue<Guid>();
        Descricao = json.Texto("descricao");
        DataOcorrencia = DateOnly.ParseExact(json.Texto("dataOcorrencia"), "yyyy-MM-dd");
        Valor = json["valor"]!.GetValue<decimal>();
        Mes = json.Texto("mes");
        Status = json.Texto("status");
    }
}
