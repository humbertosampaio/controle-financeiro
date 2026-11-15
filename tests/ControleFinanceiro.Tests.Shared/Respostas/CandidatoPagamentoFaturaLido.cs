using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>Um candidato a pagamento de fatura como o modelo o recebe em <c>listar_candidatos_pagamento_fatura</c>.</summary>
public sealed class CandidatoPagamentoFaturaLido
{
    public TransacaoLida Transacao { get; }
    public Guid IdCartao { get; }
    public string Banco { get; }
    public string Mes { get; }
    public DateOnly Vencimento { get; }
    public decimal Total { get; }

    public CandidatoPagamentoFaturaLido(JsonObject json)
    {
        Transacao = new TransacaoLida(json["transacao"]!.AsObject());
        IdCartao = json["idCartao"]!.GetValue<Guid>();
        Banco = json.Texto("banco");
        Mes = json.Texto("mes");
        Vencimento = DateOnly.ParseExact(json.Texto("vencimento"), "yyyy-MM-dd");
        Total = json["total"]!.GetValue<decimal>();
    }
}
