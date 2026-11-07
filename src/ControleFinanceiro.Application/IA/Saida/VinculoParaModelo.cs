using System.Globalization;
using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O vínculo de uma transação a uma despesa, já aplicado pelo domínio, ou só o erro que o impediu. É também a prévia que
/// o usuário confirma antes de gravar: o que ele vê é exatamente o que vai ser gravado.
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

    /// <summary>O texto da confirmação pedida ao usuário, só com texto mascarado, sem ids. Não vai no JSON.</summary>
    [JsonIgnore]
    public string? Mensagem { get; }

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
        Mensagem = MontarMensagem(dataTransacao, valorTransacao, descricaoTransacao, contraparteTransacao, descricaoDespesa, mes, valorProjetado);
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

    private static string MontarMensagem(
        DateOnly dataTransacao,
        Reais valorTransacao,
        DocumentoMascarado descricaoTransacao,
        DocumentoMascarado contraparteTransacao,
        DocumentoMascarado descricaoDespesa,
        MesReferencia mes,
        Reais valorProjetado)
    {
        string contraparte = contraparteTransacao.TemConteudo ? $" ({contraparteTransacao})" : string.Empty;

        return $"Vincular a saída de {valorTransacao} em {dataTransacao.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}, "
            + $"\"{descricaoTransacao.ConteudoOuVazio}\"{contraparte}, à despesa \"{descricaoDespesa.ConteudoOuVazio}\" "
            + $"de {mes}, com valor previsto de {valorProjetado}?";
    }
}
