using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O que o modelo vê de um cartão: o bastante para achar a fatura e para o usuário reconhecer o cartão. Os cartões do
/// mesmo banco com os mesmos dias saem na mesma fatura, e qualquer um deles a identifica.
/// </summary>
public sealed record CartaoParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    /// <summary>O nome do banco emissor, como <c>C6 Bank</c>.</summary>
    [JsonPropertyName("banco")]
    public string Banco { get; }

    /// <summary>Os 4 últimos dígitos do cartão, nunca o número inteiro.</summary>
    [JsonPropertyName("final")]
    public string Final { get; }

    /// <summary>O último dia de compras da fatura.</summary>
    [JsonPropertyName("diaFechamento")]
    public int DiaFechamento { get; }

    [JsonPropertyName("diaVencimento")]
    public int DiaVencimento { get; }

    private CartaoParaModelo(IdCartao id, Banco banco, SufixoCartao final, DiaFechamentoCartao diaFechamento, DiaVencimentoCartao diaVencimento)
    {
        Id = id.Value;
        Banco = banco.Name;
        Final = final;
        DiaFechamento = diaFechamento;
        DiaVencimento = diaVencimento;
    }

    public static CartaoParaModelo De(Cartao cartao) => new(cartao.Id, cartao.Banco, cartao.Sufixo, cartao.DiaFechamento, cartao.DiaVencimento);
}
