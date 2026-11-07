using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>O vínculo de uma transação a uma despesa, como o modelo o recebe quando ele é gravado.</summary>
public sealed class VinculoLido
{
    public TransacaoLida Transacao { get; }
    public DespesaDoMesLida Despesa { get; }
    public decimal ValorProjetado { get; }

    public VinculoLido(JsonObject json)
    {
        Transacao = new TransacaoLida(json["transacao"]!.AsObject());
        Despesa = new DespesaDoMesLida(json["despesa"]!.AsObject());
        ValorProjetado = json["valorProjetado"]!.GetValue<decimal>();
    }
}
