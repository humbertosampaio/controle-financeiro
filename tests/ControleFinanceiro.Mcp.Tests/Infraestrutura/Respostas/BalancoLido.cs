using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;

/// <summary>
/// A resposta do <c>balanco_mensal</c> num mês válido. Os nomes do JSON divergem das propriedades do tipo de saída
/// (<c>TotalPago</c>, <c>TotalPendente</c>, <c>SaldoPrevisto</c>): ler <c>pago</c>, <c>pendente</c> e <c>saldo</c> prova
/// que os atributos chegaram ao modelo.
/// </summary>
internal sealed class BalancoLido
{
    public string Mes { get; }
    public decimal RendaLiquida { get; }
    public decimal Pago { get; }
    public decimal Pendente { get; }
    public decimal Saldo { get; }

    public BalancoLido(JsonObject json)
    {
        Mes = json.Texto("mes");
        RendaLiquida = json["rendaLiquida"]!.GetValue<decimal>();
        Pago = json["pago"]!.GetValue<decimal>();
        Pendente = json["pendente"]!.GetValue<decimal>();
        Saldo = json["saldo"]!.GetValue<decimal>();
    }
}
