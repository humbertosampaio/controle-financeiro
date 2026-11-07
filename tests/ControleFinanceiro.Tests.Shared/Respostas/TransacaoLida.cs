using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>Uma transação como o modelo a recebe: um campo ausente ou de outro tipo falha na leitura.</summary>
public sealed class TransacaoLida
{
    public Guid Id { get; }
    public DateOnly Data { get; }
    public decimal Valor { get; }
    public string Descricao { get; }
    public string? Contraparte { get; }

    public TransacaoLida(JsonObject json)
    {
        Id = json["id"]!.GetValue<Guid>();
        Data = DateOnly.ParseExact(json.Texto("data"), "yyyy-MM-dd");
        Valor = json["valor"]!.GetValue<decimal>();
        Descricao = json.Texto("descricao");
        Contraparte = json["contraparte"]?.GetValue<string>();
    }
}
