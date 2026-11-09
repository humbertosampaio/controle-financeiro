using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O vínculo de uma transação a uma despesa, já aplicado pelo domínio, ou só o erro que o impediu. É também a prévia que
/// o usuário aprova antes de gravar: a confirmação só grava se o vínculo recalculado tiver o mesmo resumo que ele leu.
/// </summary>
public sealed record VinculoParaModelo : ISaidaModelo
{
    [JsonPropertyName("transacao")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TransacaoParaModelo? Transacao { get; }

    /// <summary>
    /// A despesa no mês que a transação pagou ou cobrou. A do cartão, cobrada pela compra, continua pendente, com o valor
    /// cobrado.
    /// </summary>
    [JsonPropertyName("despesa")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DespesaParaModelo? Despesa { get; }

    /// <summary>O valor previsto do pagamento, para comparar com o da transação.</summary>
    [JsonPropertyName("valorProjetado")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? ValorProjetado { get; }

    [JsonPropertyName("erro")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErroParaModelo? Erro { get; }

    /// <summary>
    /// O vínculo em uma frase, só com texto mascarado, sem ids e com espaços comuns, para o usuário ler no pedido de
    /// aprovação da confirmação.
    /// </summary>
    [JsonPropertyName("resumo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Resumo { get; }

    private VinculoParaModelo(
        TransacaoParaModelo transacao,
        DespesaParaModelo despesa,
        Reais valorProjetado,
        DateOnly dataTransacao,
        Reais valorTransacao,
        DocumentoMascarado descricaoTransacao,
        DocumentoMascarado contraparteTransacao,
        DocumentoMascarado descricaoDespesa,
        MesReferencia mes,
        DocumentoMascarado sujeito,
        DocumentoMascarado efeito)
    {
        Transacao = transacao;
        Despesa = despesa;
        ValorProjetado = valorProjetado;
        Resumo = MontarResumo(dataTransacao, valorTransacao, descricaoTransacao, contraparteTransacao, descricaoDespesa, mes, valorProjetado, sujeito, efeito);
    }

    private VinculoParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    /// <param name="despesa">A despesa já vinculada à transação.</param>
    /// <exception cref="ArgumentException">Quando a transação não cobrou nem pagou nenhum pagamento da despesa.</exception>
    public static VinculoParaModelo De(Transacao transacao, Despesa despesa)
    {
        Maybe<Pagamento> cobrado = despesa.PagamentoCobradoPor(transacao.Id);
        Pagamento pagamento = (cobrado.HasValue ? cobrado : despesa.PagamentosRealizadosPor(transacao.Id).TryFirst())
            .GetValueOrThrow(new ArgumentException("A transação não cobrou nem pagou nenhum pagamento da despesa.", nameof(despesa)));

        // Texto fixo, como o do ErroParaModelo: passa pela máscara como qualquer texto.
        (DocumentoMascarado sujeito, DocumentoMascarado efeito) = cobrado.HasValue
            ? ((DocumentoMascarado)"A compra", (DocumentoMascarado)"cobra")
            : ((DocumentoMascarado)"A saída", (DocumentoMascarado)"paga");

        return new VinculoParaModelo(
            transacao.ParaModelo(),
            despesa.ParaModelo(pagamento),
            pagamento.ValorProjetado,
            transacao.Data,
            transacao.ValorAbsoluto,
            (DocumentoMascarado)transacao.Descricao,
            (DocumentoMascarado)transacao.Contraparte,
            (DocumentoMascarado)despesa.Descricao,
            pagamento.MesReferencia,
            sujeito,
            efeito);
    }

    public static VinculoParaModelo Falha(ErroParaModelo erro) => new(erro);

    /// <summary>Se o resumo que o usuário aprovou é o deste vínculo, pelo <see cref="ResumoAprovado.Confere"/>.</summary>
    public bool ConfereResumo(string resumo) => ResumoAprovado.Confere(Resumo, resumo);

    private static string MontarResumo(
        DateOnly dataTransacao,
        Reais valorTransacao,
        DocumentoMascarado descricaoTransacao,
        DocumentoMascarado contraparteTransacao,
        DocumentoMascarado descricaoDespesa,
        MesReferencia mes,
        Reais valorProjetado,
        DocumentoMascarado sujeito,
        DocumentoMascarado efeito)
    {
        string contraparte = contraparteTransacao.TemConteudo ? $" ({contraparteTransacao})" : string.Empty;

        return ResumoAprovado.Normalizar(
            $"{sujeito} de {valorTransacao} em {dataTransacao.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}, "
            + $"\"{descricaoTransacao.ConteudoOuVazio}\"{contraparte}, {efeito} a despesa \"{descricaoDespesa.ConteudoOuVazio}\" "
            + $"de {mes}, com valor previsto de {valorProjetado}.");
    }
}
