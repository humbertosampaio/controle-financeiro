using System.Globalization;
using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

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

    /// <summary>A despesa no mês que a transação quitou, já paga por ela.</summary>
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
        MesReferencia mes)
    {
        Transacao = transacao;
        Despesa = despesa;
        ValorProjetado = valorProjetado;
        Resumo = MontarResumo(dataTransacao, valorTransacao, descricaoTransacao, contraparteTransacao, descricaoDespesa, mes, valorProjetado);
    }

    private VinculoParaModelo(ErroParaModelo erro)
    {
        Erro = erro;
    }

    /// <param name="despesa">A despesa já vinculada à transação.</param>
    /// <exception cref="ArgumentException">Quando a transação não quitou nenhum pagamento da despesa.</exception>
    public static VinculoParaModelo De(Transacao transacao, Despesa despesa)
    {
        MesReferencia mes = despesa
            .MesQuitado(transacao.Id)
            .GetValueOrThrow(new ArgumentException("A transação não quitou nenhum pagamento da despesa.", nameof(despesa)));
        // O Value é seguro: cada parcela cai num mês diferente. A #136 elimina esta segunda busca.
        Pagamento pagamento = despesa.PagamentoDoMes(mes).Value;

        return new VinculoParaModelo(
            transacao.ParaModelo(),
            despesa.ParaModelo(mes),
            pagamento.ValorProjetado,
            transacao.Data,
            transacao.ValorAbsoluto,
            (DocumentoMascarado)transacao.Descricao,
            (DocumentoMascarado)transacao.Contraparte,
            (DocumentoMascarado)despesa.Descricao,
            mes);
    }

    public static VinculoParaModelo Falha(ErroParaModelo erro) => new(erro);

    /// <summary>
    /// Se o resumo que o usuário aprovou é o deste vínculo. O cliente pode trocar um espaço por outro, ou acrescentar
    /// espaços nas pontas, e isso não muda o que o usuário leu.
    /// </summary>
    public bool ConfereResumo(string resumo) => Resumo is not null && string.Equals(NormalizarEspacos(resumo), Resumo, StringComparison.Ordinal);

    private static string MontarResumo(
        DateOnly dataTransacao,
        Reais valorTransacao,
        DocumentoMascarado descricaoTransacao,
        DocumentoMascarado contraparteTransacao,
        DocumentoMascarado descricaoDespesa,
        MesReferencia mes,
        Reais valorProjetado)
    {
        string contraparte = contraparteTransacao.TemConteudo ? $" ({contraparteTransacao})" : string.Empty;

        // O Reais formata com espaço não separável, e o resumo volta nos argumentos da confirmação.
        return NormalizarEspacos(
            $"A saída de {valorTransacao} em {dataTransacao.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}, "
            + $"\"{descricaoTransacao.ConteudoOuVazio}\"{contraparte}, paga a despesa \"{descricaoDespesa.ConteudoOuVazio}\" "
            + $"de {mes}, com valor previsto de {valorProjetado}.");
    }

    private static string NormalizarEspacos(string texto)
    {
        string comEspacosComuns = string.Concat(texto.Select(c => char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator ? ' ' : c));
        return comEspacosComuns.Trim();
    }
}
