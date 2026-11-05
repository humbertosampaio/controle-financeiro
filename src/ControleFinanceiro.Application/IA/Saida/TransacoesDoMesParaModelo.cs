using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>As transações de um mês, ou só o erro que impediu de listá-las.</summary>
public sealed record TransacoesDoMesParaModelo : ISaidaModelo
{
    /// <summary>O mês, como <c>03/2026</c>.</summary>
    [JsonPropertyName("mes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Mes { get; }

    [JsonPropertyName("transacoes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TransacaoParaModelo>? Transacoes { get; }

    [JsonPropertyName("erro")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErroParaModelo? Erro { get; }

    private TransacoesDoMesParaModelo(MesReferencia mes, IReadOnlyList<TransacaoParaModelo> transacoes)
    {
        Mes = mes.ToString();
        Transacoes = transacoes;
    }

    private TransacoesDoMesParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    public static TransacoesDoMesParaModelo De(MesReferencia mes, IReadOnlyList<TransacaoParaModelo> transacoes) => new(mes, transacoes);

    public static TransacoesDoMesParaModelo Falha(ErroParaModelo erro) => new(erro);
}
