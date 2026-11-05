using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.SaidaModelo;

/// <summary>O balanço de um mês, só com totais, ou só o erro que impediu de calculá-lo.</summary>
public sealed record BalancoMensalParaModelo : ISaidaModelo
{
    /// <summary>O mês, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mes { get; }

    /// <summary>A renda atual do usuário, sem histórico: um mês passado usa a de hoje.</summary>
    [JsonPropertyName("rendaLiquida")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? RendaLiquida { get; }

    [JsonPropertyName("pago")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TotalPago { get; }

    [JsonPropertyName("pendente")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TotalPendente { get; }

    /// <summary>A renda líquida menos o pago e o pendente, negativo quando as despesas passam dela.</summary>
    [JsonPropertyName("saldo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? SaldoPrevisto { get; }

    [JsonPropertyName("erro")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErroParaModelo? Erro { get; }

    private BalancoMensalParaModelo(MesReferencia mes, Reais rendaLiquida, Reais totalPago, Reais totalPendente, Reais saldoPrevisto)
    {
        Mes = mes.ToString();
        RendaLiquida = rendaLiquida;
        TotalPago = totalPago;
        TotalPendente = totalPendente;
        SaldoPrevisto = saldoPrevisto;
    }

    private BalancoMensalParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    public static BalancoMensalParaModelo De(BalancoMensal balanco)
    {
        return new BalancoMensalParaModelo(
            balanco.Mes,
            balanco.RendaLiquida,
            balanco.TotalPago,
            balanco.TotalPendente,
            balanco.SaldoPrevisto);
    }

    public static BalancoMensalParaModelo Falha(ErroParaModelo erro) => new(erro);
}
