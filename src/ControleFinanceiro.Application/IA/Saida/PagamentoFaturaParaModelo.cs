using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O pagamento da fatura do cartão por uma saída da conta, já aplicado pelo domínio, ou só o erro que o impediu. É também
/// a prévia que o usuário aprova antes de gravar: a confirmação só grava se o pagamento recalculado tiver o mesmo resumo
/// que ele leu.
/// </summary>
public sealed record PagamentoFaturaParaModelo : ISaidaModelo
{
    /// <summary>A saída da conta que paga a fatura.</summary>
    [JsonPropertyName("transacao")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransacaoParaModelo? Transacao { get; }

    /// <summary>As despesas que a saída paga nesta chamada, no mês de vencimento, já pagas com o valor cobrado.</summary>
    [JsonPropertyName("despesas")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DespesaParaModelo>? Despesas { get; }

    /// <summary>A soma do que a saída paga nesta chamada, para comparar com o valor dela.</summary>
    [JsonPropertyName("total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Total { get; }

    [JsonPropertyName("erro")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErroParaModelo? Erro { get; }

    /// <summary>
    /// O pagamento em uma frase, só com texto mascarado, o nome do banco, o final do cartão informado e o mês de
    /// vencimento, sem ids e com espaços comuns, para o usuário ler no pedido de aprovação da confirmação.
    /// </summary>
    [JsonPropertyName("resumo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Resumo { get; }

    private PagamentoFaturaParaModelo(
        TransacaoParaModelo transacao,
        IReadOnlyList<DespesaParaModelo> despesas,
        Reais total,
        DateOnly dataSaida,
        Reais valorSaida,
        DocumentoMascarado descricaoSaida,
        DocumentoMascarado contraparteSaida,
        Banco banco,
        SufixoCartao finalCartao,
        MesReferencia mesVencimento)
    {
        Transacao = transacao;
        Despesas = despesas;
        Total = total;
        Resumo = MontarResumo(dataSaida, valorSaida, descricaoSaida, contraparteSaida, banco, finalCartao, mesVencimento, despesas.Count, total);
    }

    private PagamentoFaturaParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    /// <param name="saida">A saída que pagou a fatura.</param>
    /// <param name="cartao">O cartão da fatura que o modelo informou.</param>
    /// <param name="pagamento">O que a saída pagou na fatura.</param>
    public static PagamentoFaturaParaModelo De(Transacao saida, Cartao cartao, PagamentoFatura pagamento)
    {
        return new PagamentoFaturaParaModelo(
            saida.ParaModelo(),
            pagamento.Pagas.Select(despesa => despesa.ParaModelo(pagamento.MesVencimento)).ToArray(),
            pagamento.Total,
            saida.Data,
            saida.ValorAbsoluto,
            (DocumentoMascarado)saida.Descricao,
            (DocumentoMascarado)saida.Contraparte,
            cartao.Banco,
            cartao.Sufixo,
            pagamento.MesVencimento);
    }

    public static PagamentoFaturaParaModelo Falha(ErroParaModelo erro) => new(erro);

    /// <summary>Se o resumo que o usuário aprovou é o deste pagamento, pelo <see cref="ResumoAprovado.Confere"/>.</summary>
    public bool ConfereResumo(string resumo) => ResumoAprovado.Confere(Resumo, resumo);

    private static string MontarResumo(
        DateOnly dataSaida,
        Reais valorSaida,
        DocumentoMascarado descricaoSaida,
        DocumentoMascarado contraparteSaida,
        Banco banco,
        SufixoCartao finalCartao,
        MesReferencia mesVencimento,
        int quantidadeDespesas,
        Reais total)
    {
        string contraparte = contraparteSaida.TemConteudo ? $" ({contraparteSaida})" : string.Empty;
        string despesas = quantidadeDespesas == 1 ? "1 despesa" : $"{quantidadeDespesas} despesas";

        return ResumoAprovado.Normalizar(
            $"A saída de {valorSaida} em {dataSaida.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}, "
            + $"\"{descricaoSaida.ConteudoOuVazio}\"{contraparte}, paga a fatura do {banco.Name} em que sai o cartão final {finalCartao}, "
            + $"com vencimento em {mesVencimento}: "
            + $"{despesas}, no total de {total}.");
    }
}
