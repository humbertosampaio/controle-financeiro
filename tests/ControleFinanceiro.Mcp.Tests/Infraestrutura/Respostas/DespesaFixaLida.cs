using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;

/// <summary>Uma despesa fixa como o modelo a recebe, com a despesa do mês quando ela já foi gerada.</summary>
internal sealed class DespesaFixaLida
{
    public Guid Id { get; }
    public string Descricao { get; }
    public decimal Valor { get; }
    public int DiaOcorrencia { get; }
    public DespesaDoMesLida? DespesaDoMes { get; }

    public DespesaFixaLida(JsonObject json)
    {
        Id = json["id"]!.GetValue<Guid>();
        Descricao = json.Texto("descricao");
        Valor = json["valor"]!.GetValue<decimal>();
        DiaOcorrencia = json["diaOcorrencia"]!.GetValue<int>();
        DespesaDoMes = json["despesaDoMes"] is JsonObject despesaDoMes ? new DespesaDoMesLida(despesaDoMes) : null;
    }
}
