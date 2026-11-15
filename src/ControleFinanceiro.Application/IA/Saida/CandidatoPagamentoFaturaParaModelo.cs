using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O que o modelo vê de um candidato a pagamento de fatura: a saída da conta e o bastante para pedir o pagamento ao
/// <see cref="Tools.PreverPagamentoFatura"/>. Não vão o final do cartão, o sufixo da conta, a conta nem a agência.
/// </summary>
public sealed record CandidatoPagamentoFaturaParaModelo : ISaidaModelo
{
    /// <summary>A saída da conta que provavelmente pagou a fatura.</summary>
    [JsonPropertyName("transacao")]
    public TransacaoParaModelo Transacao { get; }

    /// <summary>Um cartão da fatura, o id que o <see cref="Tools.PreverPagamentoFatura"/> pede.</summary>
    [JsonPropertyName("idCartao")]
    public Guid IdCartao { get; }

    /// <summary>O nome do banco emissor, como <c>C6 Bank</c>.</summary>
    [JsonPropertyName("banco")]
    public string Banco { get; }

    /// <summary>O mês em que a fatura vence, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    public string Mes { get; }

    /// <summary>A data em que a fatura vence, a que a importação dela gravou.</summary>
    [JsonPropertyName("vencimento")]
    public DateOnly Vencimento { get; }

    /// <summary>O que a fatura cobra, igual ao valor da saída.</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; }

    private CandidatoPagamentoFaturaParaModelo(
        TransacaoParaModelo transacao,
        IdCartao idCartao,
        Banco banco,
        MesReferencia mes,
        DateOnly vencimento,
        Reais total)
    {
        Transacao = transacao;
        IdCartao = idCartao.Value;
        Banco = banco.Name;
        Mes = mes.ToString();
        Vencimento = vencimento;
        Total = total;
    }

    public static CandidatoPagamentoFaturaParaModelo De(CandidatoPagamentoFaturaDto candidatoDto)
    {
        CandidatoPagamentoFatura candidato = candidatoDto.Candidato;

        return new CandidatoPagamentoFaturaParaModelo(
            candidatoDto.Saida.ParaModelo(),
            candidato.IdCartao,
            candidato.FaturaCartaoAberta.Fatura.Banco,
            candidato.FaturaCartaoAberta.Vencimento.Mes,
            candidato.FaturaCartaoAberta.Vencimento,
            candidato.FaturaCartaoAberta.Total);
    }
}
