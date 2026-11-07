using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O que o modelo vê de uma despesa, de qualquer tipo, num mês: o alvo de um vínculo. O valor e o status são os do
/// pagamento que cai no mês, que na parcelada é o da parcela dele.
/// </summary>
public sealed record DespesaParaModelo : ISaidaModelo
{
    [JsonPropertyName("id")]
    public Guid Id { get; }

    /// <summary>Vazia quando só havia documento nela.</summary>
    [JsonPropertyName("descricao")]
    public string Descricao { get; }

    [JsonPropertyName("dataOcorrencia")]
    public DateOnly DataOcorrencia { get; }

    /// <summary>O valor efetivo quando paga, o projetado quando pendente.</summary>
    [JsonPropertyName("valor")]
    public decimal Valor { get; }

    /// <summary>O mês do pagamento, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    public string Mes { get; }

    [JsonPropertyName("status")]
    public string Status { get; }

    private DespesaParaModelo(
        IdDespesa id,
        DocumentoMascarado descricao,
        DateOnly dataOcorrencia,
        Reais valor,
        MesReferencia mes,
        StatusPagamento status)
    {
        Id = id.Value;
        Descricao = descricao.ConteudoOuVazio;
        DataOcorrencia = dataOcorrencia;
        Valor = valor;
        Mes = mes.ToString();
        Status = status.Name;
    }

    /// <exception cref="ArgumentException">Quando a despesa não tem pagamento no mês.</exception>
    public static DespesaParaModelo De(Despesa despesa, MesReferencia mes)
    {
        Pagamento pagamento = despesa
            .PagamentoDoMes(mes)
            .GetValueOrThrow(new ArgumentException("A despesa não tem pagamento no mês.", nameof(despesa)));

        return De(despesa, pagamento);
    }

    /// <param name="pagamento">Um pagamento da despesa.</param>
    /// <remarks>
    /// O valor é o vigente do mês do pagamento: a despesa do cartão cobrada e ainda não paga sai como pendente, com o
    /// valor cobrado.
    /// </remarks>
    internal static DespesaParaModelo De(Despesa despesa, Pagamento pagamento)
    {
        return new DespesaParaModelo(
            despesa.Id,
            (DocumentoMascarado)despesa.Descricao,
            despesa.DataOcorrencia,
            despesa.ValorVigenteDoMes(pagamento.MesReferencia).Value,
            pagamento.MesReferencia,
            pagamento.Status);
    }
}
