using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>O pagamento da fatura por uma saída, como o modelo o recebe na prévia e na confirmação.</summary>
public sealed class PagamentoFaturaLido
{
    public TransacaoLida Transacao { get; }
    public IReadOnlyList<DespesaDoMesLida> Despesas { get; }
    public decimal Total { get; }
    public string Resumo { get; }

    public PagamentoFaturaLido(JsonObject json)
    {
        Transacao = new TransacaoLida(json["transacao"]!.AsObject());
        Despesas = json["despesas"]!
            .AsArray()
            .Select(despesa => new DespesaDoMesLida(despesa!.AsObject()))
            .ToArray();
        Total = json["total"]!.GetValue<decimal>();
        Resumo = json.Texto("resumo");
    }
}
