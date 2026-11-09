using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>Um cartão como o modelo o recebe em <c>listar_cartoes</c>.</summary>
public sealed class CartaoLido
{
    public Guid Id { get; }
    public string Banco { get; }
    public string Final { get; }
    public int DiaFechamento { get; }
    public int DiaVencimento { get; }

    public CartaoLido(JsonObject json)
    {
        Id = json["id"]!.GetValue<Guid>();
        Banco = json.Texto("banco");
        Final = json.Texto("final");
        DiaFechamento = json["diaFechamento"]!.GetValue<int>();
        DiaVencimento = json["diaVencimento"]!.GetValue<int>();
    }
}
