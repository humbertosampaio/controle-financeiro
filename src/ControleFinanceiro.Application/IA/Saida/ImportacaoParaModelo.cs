using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// A importação de um extrato ou de uma fatura, já aplicada em memória, ou o erro que a impediu, com o que o usuário
/// precisa resolver. Só leva contagens, datas, o banco, o layout, os finais de cartão e, no resumo, o final da conta:
/// nenhuma linha do arquivo, nem o nome dele, nem o número inteiro da conta. É também a prévia que o usuário aprova: a confirmação só grava se a importação
/// recalculada tiver o mesmo resumo que ele leu.
/// </summary>
public sealed record ImportacaoParaModelo : ISaidaModelo
{
    private const string AvisoSomaPeriodo =
        "A soma das transações gravadas no período não fecha com a do arquivo: confira o extrato pela API.";

    /// <summary>O código do layout, como o modelo o informou.</summary>
    [JsonPropertyName("layout")]
    public string? Layout { get; }

    [JsonPropertyName("banco")]
    public string? Banco { get; }

    /// <summary>O período do arquivo; na fatura, do primeiro ao último lançamento de todos os cartões.</summary>
    [JsonPropertyName("periodo")]
    public PeriodoParaModelo? Periodo { get; }

    /// <summary>Só no extrato: verdadeiro quando a importação cria a conta.</summary>
    [JsonPropertyName("conta_nova")]
    public bool? ContaNova { get; }

    /// <summary>Só na fatura: um item por final de cartão, na ordem do arquivo.</summary>
    [JsonPropertyName("cartoes")]
    public IReadOnlyList<CartaoImportadoParaModelo>? Cartoes { get; }

    [JsonPropertyName("lidas")]
    public int? Lidas { get; }

    [JsonPropertyName("novas")]
    public int? Novas { get; }

    /// <summary>As lidas que já estavam gravadas, de uma importação anterior.</summary>
    [JsonPropertyName("ja_existentes")]
    public int? JaExistentes { get; }

    /// <summary>Só no extrato: os dias entre a importação anterior da conta e esta que nenhum arquivo cobriu.</summary>
    [JsonPropertyName("periodo_nao_importado")]
    public PeriodoParaModelo? PeriodoNaoImportado { get; }

    /// <summary>Avisos fixos, que não impedem a importação.</summary>
    [JsonPropertyName("avisos")]
    public IReadOnlyList<string>? Avisos { get; }

    /// <summary>Na falha que o usuário resolve pela API: o que falta cadastrar.</summary>
    [JsonPropertyName("pendencias")]
    public PendenciasImportacaoParaModelo? Pendencias { get; }

    [JsonPropertyName("erro")]
    public ErroParaModelo? Erro { get; }

    /// <summary>
    /// A importação em uma frase, sem o caminho, sem ids e sem horário, com espaços comuns, para o usuário ler no pedido
    /// de aprovação da confirmação.
    /// </summary>
    [JsonPropertyName("resumo")]
    public string? Resumo { get; }

    private ImportacaoParaModelo(
        LayoutArquivoImportacao layout,
        Banco banco,
        TipoConta tipoConta,
        SufixoConta finalConta,
        PeriodoArquivo periodo,
        bool contaNova,
        int lidas,
        int novas,
        int jaExistentes,
        PeriodoParaModelo? periodoNaoImportado,
        bool somaPeriodoFecha)
    {
        Layout = layout.Value;
        Banco = banco.Name;
        Periodo = PeriodoParaModelo.De(periodo);
        ContaNova = contaNova;
        Lidas = lidas;
        Novas = novas;
        JaExistentes = jaExistentes;
        PeriodoNaoImportado = periodoNaoImportado;
        Avisos = somaPeriodoFecha ? null : [AvisoSomaPeriodo];

        // O final da conta amarra a confirmação à conta da prévia: no layout em que o usuário informa o sufixo, outro sufixo
        // na confirmação muda o resumo, em vez de criar a conta com o final errado.
        string conta = contaNova ? $"; cria a {Minuscula(tipoConta.Name)} final {finalConta} do {banco.Name}" : string.Empty;
        Resumo = ResumoAprovado.Normalizar(
            $"Importa o extrato {layout.Value} da conta final {finalConta} do {banco.Name}, {Datas(periodo)}: "
            + $"{Contagens(lidas, novas, jaExistentes)}{conta}.");
    }

    private ImportacaoParaModelo(
        LayoutArquivoImportacao layout,
        Banco banco,
        PeriodoArquivo periodo,
        IReadOnlyList<CartaoImportadoParaModelo> cartoes,
        bool trazPagamentoFaturaAnterior)
    {
        Layout = layout.Value;
        Banco = banco.Name;
        Periodo = PeriodoParaModelo.De(periodo);
        Cartoes = cartoes;
        Lidas = cartoes.Sum(cartao => cartao.Lidas);
        Novas = cartoes.Sum(cartao => cartao.Novas);
        JaExistentes = cartoes.Sum(cartao => cartao.JaExistentes);
        Avisos = trazPagamentoFaturaAnterior ? null : [Fatura.AvisoSemPagamentoAnterior];

        Resumo = ResumoAprovado.Normalizar(
            $"Importa a fatura {layout.Value} do {banco.Name}, {Datas(periodo)}: "
            + string.Join("; ", cartoes.Select(DescreverCartao))
            + ".");
    }

    private ImportacaoParaModelo(ErroParaModelo erro, PendenciasImportacaoParaModelo? pendencias)
    {
        Erro = erro;
        Pendencias = pendencias;
    }

    public static ImportacaoParaModelo DoExtrato(LayoutArquivoImportacao layout, ExtratoImportado importado)
    {
        return new ImportacaoParaModelo(
            layout,
            importado.Banco,
            importado.TipoConta,
            importado.SufixoConta,
            importado.Periodo,
            importado.ContaNova,
            importado.Lidas,
            importado.Novas,
            importado.JaExistentes,
            importado.PeriodoNaoImportado.Map(PeriodoParaModelo.De).GetValueOrDefault(),
            importado.SomaPeriodo.IsSuccess);
    }

    /// <param name="fatura">A fatura lida, de onde sai o período de todos os cartões.</param>
    public static ImportacaoParaModelo DaFatura(LayoutArquivoImportacao layout, Fatura fatura, FaturaImportada importada)
    {
        return new ImportacaoParaModelo(
            layout,
            importada.Banco,
            fatura.Periodo,
            importada.Cartoes.Select(CartaoImportadoParaModelo.De).ToArray(),
            importada.PagamentoFaturaAnterior.IsSuccess);
    }

    public static ImportacaoParaModelo Falha(ErroParaModelo erro) => new(erro, pendencias: null);

    public static ImportacaoParaModelo Falha(ErroParaModelo erro, PendenciasImportacaoParaModelo pendencias) => new(erro, pendencias);

    /// <summary>Se o resumo que o usuário aprovou é o desta importação, pelo <see cref="ResumoAprovado.Confere"/>.</summary>
    public bool ConfereResumo(string resumo) => ResumoAprovado.Confere(Resumo, resumo);

    private static string DescreverCartao(CartaoImportadoParaModelo cartao)
    {
        string parceladas = cartao.DespesasParceladas == 1 ? "1 despesa parcelada" : $"{cartao.DespesasParceladas} despesas parceladas";
        string adiadas = cartao.DespesasAdiadas == 1 ? "1 adiada" : $"{cartao.DespesasAdiadas} adiadas";

        return $"cartão final {cartao.Final}, {Contagens(cartao.Lidas, cartao.Novas, cartao.JaExistentes)}, cria {parceladas} e {adiadas}";
    }

    private static string Contagens(int lidas, int novas, int jaExistentes) => $"{lidas} lidas, {novas} novas e {jaExistentes} já existentes";

    private static string Datas(PeriodoArquivo periodo) => $"de {Data(periodo.Inicio)} a {Data(periodo.Fim)}";

    private static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Minuscula(string texto) => string.Concat(char.ToLowerInvariant(texto[0]), texto[1..]);
}
