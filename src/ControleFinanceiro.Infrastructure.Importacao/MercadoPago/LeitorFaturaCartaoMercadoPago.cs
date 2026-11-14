using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LancamentoLido = (ControleFinanceiro.Domain.Transacoes.Lancamento Lancamento, ControleFinanceiro.Domain.Cartoes.SufixoCartao? Sufixo);
using TipoEValor = (ControleFinanceiro.Infrastructure.Importacao.MercadoPago.LeitorFaturaCartaoMercadoPago.TipoLancamento Tipo, ControleFinanceiro.Domain.Shared.ValueObjects.Reais Valor);
using TipoValorSufixoEParcela = (ControleFinanceiro.Infrastructure.Importacao.MercadoPago.LeitorFaturaCartaoMercadoPago.TipoLancamento Tipo, ControleFinanceiro.Domain.Shared.ValueObjects.Reais Valor, ControleFinanceiro.Domain.Cartoes.SufixoCartao? Sufixo, ControleFinanceiro.Domain.Transacoes.Parcela? Parcela);

namespace ControleFinanceiro.Infrastructure.Importacao.MercadoPago;

/// <summary>
/// Lê a fatura CSV do cartão do Mercado Pago, que o Mago gera com o prompt de <c>docs/fatura-mercado-pago.md</c>: as
/// 19 colunas do prompt na primeira linha, separadas por <c>;</c>, e depois um lançamento por linha.
/// </summary>
/// <remarks>
/// <para>
/// Como o arquivo é gerado por IA, o leitor não dá palpite: as colunas têm de ser as do prompt, na ordem, e o tipo de
/// lançamento tem de ser um dos conhecidos, que diz a coluna do valor, se o final do cartão é obrigatório e se a linha é
/// o pagamento da fatura anterior. Um tipo novo entra na lista quando aparecer num arquivo, declarando se é pagamento.
/// </para>
/// <para>
/// O lançamento é reconhecido numa reimportação pelo <c>numero_transacao</c>, pela parcela e pelo valor
/// (<see cref="IdentidadeLancamento.Composta(string)"/>): a parcela seguinte da mesma compra, na fatura seguinte,
/// traz o mesmo número.
/// </para>
/// <para>
/// O que o arquivo traz, pelo prompt e pelas faturas reais, e de que o leitor depende:
/// <list type="bullet">
/// <item>os valores vêm sem sinal: a compra na coluna da cobrança, e o crédito na do crédito, com a outra coluna vazia;</item>
/// <item>o tipo <c>pagamento_debito_automatico_pos_fechamento</c> é o pagamento da fatura anterior: vem na coluna do
/// crédito, sem final e sem parcela, e é lido pelo <see cref="Lancamento.PagamentoFaturaCartao"/>, com a marca de
/// pagamento da fatura;</item>
/// <item>o pagamento vai para o menor final do arquivo, para que a escolha não dependa da ordem das linhas;</item>
/// <item>nenhum tipo conhecido é estorno: o primeiro que aparecer entra na lista como crédito, sem a marca.</item>
/// </list>
/// </para>
/// <para>
/// O CPF e o CNPJ da descrição e do vendedor ficam: quem os tira é a saída para o modelo. Os produtos e o número do
/// pedido não são mapeados, nem o mês de referência, o vencimento, o fechamento, o valor original da compra e o
/// status. As mensagens de falha citam só o número da linha, e a <see cref="CsvFormatException"/>, que pode trazer dados do
/// registro, é capturada.
/// </para>
/// </remarks>
public sealed partial class LeitorFaturaCartaoMercadoPago : ILeitorFatura
{
    private const string MoedaReais = "BRL";

    private static readonly string[] _colunas =
    [
        "mes_referencia", "vencimento", "fechamento", "data_compra", "data_lancamento", "tipo_lancamento",
        "descricao_fatura", "vendedor", "produtos", "numero_pedido", "numero_transacao", "cartao_final",
        "parcela_atual", "parcelas_totais", "moeda", "valor_compra_original", "valor_cobrado_fatura",
        "valor_creditado_fatura", "status",
    ];

    private static readonly Dictionary<string, TipoLancamento> _tipos = new(StringComparer.Ordinal)
    {
        ["compra_a_vista"] = new(Credito: false, FinalObrigatorio: true, Parcelado: false, PagamentoFatura: false),
        ["compra_parcelada"] = new(Credito: false, FinalObrigatorio: true, Parcelado: true, PagamentoFatura: false),
        ["pagamento_debito_automatico_pos_fechamento"] = new(Credito: true, FinalObrigatorio: false, Parcelado: false, PagamentoFatura: true),
    };

    private static readonly CsvReaderOptions _opcoes = OpcoesLeituraCsv.Criar(separador: ';');

    private static readonly FalhaLeituraArquivo _arquivoTerminaAntesColunas = FalhaLeituraArquivo.NoArquivo(
        TipoFalhaLeituraArquivo.SemColunas,
        "O arquivo termina antes das colunas da fatura do cartão do Mercado Pago.");

    private static readonly FalhaLeituraArquivo _semAsColunas = FalhaLeituraArquivo.NaLinha(
        TipoFalhaLeituraArquivo.SemColunas,
        linha: 1,
        "não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");

    public LayoutArquivoImportacao Layout => LayoutArquivoImportacao.MercadoPagoCartaoCsv;

    /// <summary>Confere as colunas, lê cada lançamento, põe o pagamento num cartão e monta a fatura.</summary>
    public async Task<Result<Fatura, FalhaLeituraArquivo>> LerAsync(Stream arquivo, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using CsvReader csv = CsvReader.Create(leitor, _opcoes);

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken))
            .Bind(lidos => PorNoMenorFinal(lidos).MapError(ConteudoInvalido))
            .Bind(lancamentos => Fatura.Criar(Banco.MercadoPago, lancamentos).MapError(ConteudoInvalido));
    }

    /// <summary>
    /// As colunas têm de ser exatamente as do prompt, na ordem e com a mesma grafia.
    /// </summary>
    private static async Task<UnitResult<FalhaLeituraArquivo>> LerColunasAsync(CsvReader csv)
    {
        try
        {
            await csv.ReadHeaderAsync();
            if (csv.Headers.Count == 0)
                return _arquivoTerminaAntesColunas;

            return UnitResult.FailureIf(!csv.Headers.SequenceEqual(_colunas, StringComparer.Ordinal), _semAsColunas);
        }
        catch (CsvFormatException)
        {
            return _semAsColunas;
        }
    }

    /// <summary>Para na primeira linha que não está no layout.</summary>
    private static async Task<Result<IReadOnlyList<LancamentoLido>, FalhaLeituraArquivo>> LerLinhasAsync(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        List<LancamentoLido> lidos = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Result<Maybe<LancamentoLido>, FalhaLeituraArquivo> proximo = await LerProximaLinhaAsync(csv)
                .Bind(linha => ConverterSeHouver(csv, linha));

            if (proximo.IsFailure)
                return proximo.Error;

            if (proximo.Value.HasNoValue)
                return lidos;

            Maybe<LancamentoLido> lancamentoLido = proximo.Value;
            lidos.Add(lancamentoLido.Value);
        }
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaFaturaMercadoPago>, FalhaLeituraArquivo>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaFaturaMercadoPago>() : Maybe<LinhaFaturaMercadoPago>.None;
        }
        catch (CsvFormatException erro)
        {
            return FalhaLeituraArquivo.NaLinha(
                TipoFalhaLeituraArquivo.ForaDoLayout,
                erro.LineNumber,
                "não está no layout da fatura do cartão do Mercado Pago.");
        }
    }

    /// <param name="linha">Vazia no fim do arquivo.</param>
    private static Result<Maybe<LancamentoLido>, FalhaLeituraArquivo> ConverterSeHouver(CsvReader csv, Maybe<LinhaFaturaMercadoPago> linha)
    {
        if (linha.HasNoValue)
            return Maybe<LancamentoLido>.None;

        return Converter(linha.Value)
            .Map(Maybe.From)
            .MapError(erro => FalhaLeituraArquivo.NaLinha(TipoFalhaLeituraArquivo.LancamentoInvalido, csv.LineNumber, erro));
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoLido> Converter(LinhaFaturaMercadoPago linha)
    {
        string descricao = linha.Descricao.Trim();

        return Result
            .FailureIf(descricao.Length == 0, "não tem descrição.")
            .Ensure(() => linha.Moeda == MoedaReais, "não traz os valores em reais (BRL).")
            .Ensure(() => (linha.DataCompra ?? linha.DataLancamento).HasValue, "não tem a data da compra nem a do lançamento.")
            .Bind(() => LerTipoEValor(linha))
            .Bind(tipoEValor => LerSufixoEParcela(linha, tipoEValor))
            .Bind(valorSufixoEParcela => Lancar(linha, descricao, valorSufixoEParcela));
    }

    /// <summary>
    /// O tipo diz a coluna do valor: ela tem de vir preenchida, e a outra vazia. A compra sai negativa, e o crédito,
    /// positivo, do ponto de vista do usuário.
    /// </summary>
    private static Result<TipoEValor> LerTipoEValor(LinhaFaturaMercadoPago linha)
    {
        if (!_tipos.TryGetValue(linha.TipoLancamento, out TipoLancamento? tipo))
            return Result.Failure<TipoEValor>("não tem um tipo de lançamento conhecido.");

        (decimal? colunaTipo, decimal? outraColuna) = tipo.Credito
            ? (linha.ValorCreditado, linha.ValorCobrado)
            : (linha.ValorCobrado, linha.ValorCreditado);

        if (colunaTipo is not decimal valor || outraColuna is not null)
            return Result.Failure<TipoEValor>("não traz o valor na coluna do tipo de lançamento.");

        return (tipo, tipo.Credito ? valor : -valor);
    }

    private static Result<TipoValorSufixoEParcela> LerSufixoEParcela(LinhaFaturaMercadoPago linha, TipoEValor tipoEValor)
    {
        return LerSufixo(linha.CartaoFinal, tipoEValor.Tipo)
            .Bind(sufixo => LerParcela(linha, tipoEValor.Tipo).Map(parcela => (tipoEValor.Tipo, tipoEValor.Valor, sufixo, parcela)));
    }

    /// <returns>Nulo quando o tipo não exige o final e a linha não o traz.</returns>
    private static Result<SufixoCartao?> LerSufixo(string cartaoFinal, TipoLancamento tipo)
    {
        if (!tipo.FinalObrigatorio && cartaoFinal.Length == 0)
            return Result.Success<SufixoCartao?>(null);

        return SufixoCartao
            .Criar(cartaoFinal)
            .Map(sufixo => (SufixoCartao?)sufixo)
            .MapError(_ => "não tem o final do cartão com 4 dígitos.");
    }

    /// <summary>
    /// Só a compra parcelada tem parcela, com as duas colunas. O <c>1/1</c> é compra à vista, sem parcela, como no C6.
    /// </summary>
    private static Result<Parcela?> LerParcela(LinhaFaturaMercadoPago linha, TipoLancamento tipo)
    {
        if (!tipo.Parcelado)
            return Result.Success<Parcela?>(null);

        if (!NumeroParcela().IsMatch(linha.ParcelaAtual) || !NumeroParcela().IsMatch(linha.ParcelasTotais))
            return Result.Failure<Parcela?>("não tem a parcela da compra parcelada.");

        int numero = int.Parse(linha.ParcelaAtual, CultureInfo.InvariantCulture);
        int total = int.Parse(linha.ParcelasTotais, CultureInfo.InvariantCulture);
        if (numero == 1 && total == 1)
            return Result.Success<Parcela?>(null);

        return Parcela
            .Criar(numero, total)
            .Map(parcela => (Parcela?)parcela)
            .MapError(_ => "não tem a parcela da compra parcelada.");
    }

    /// <param name="linha">Com a data da compra ou a do lançamento.</param>
    /// <param name="descricao">Aparada e não vazia.</param>
    private static Result<LancamentoLido> Lancar(LinhaFaturaMercadoPago linha, string descricao, TipoValorSufixoEParcela tipoValorSufixoEParcela)
    {
        return IdentidadeLancamento
            .Composta(linha.NumeroTransacao)
            .MapError(_ => "não tem o número da transação.")
            .Map(identidade => identidade.ComParcela(tipoValorSufixoEParcela.Parcela).ComValor(tipoValorSufixoEParcela.Valor))
            .Map(identidade => Montar(linha, descricao, tipoValorSufixoEParcela, identidade))
            .Map(lancamento => (lancamento, tipoValorSufixoEParcela.Sufixo));
    }

    /// <summary>
    /// O pagamento da fatura sai pelo <see cref="Lancamento.PagamentoFaturaCartao"/>, sem parcela. O valor nunca é negativo:
    /// o tipo dele é crédito, que o <see cref="LerTipoEValor"/> devolve com o sinal da coluna, e o
    /// <see cref="ValorFaturaConverter"/> recusa a coluna com sinal. Os demais tipos saem pelo <see cref="Lancamento.DoCartao"/>.
    /// </summary>
    private static Lancamento Montar(
        LinhaFaturaMercadoPago linha,
        string descricao,
        TipoValorSufixoEParcela tipoValorSufixoEParcela,
        IdentidadeLancamento identidade)
    {
        DateOnly data = linha.DataCompra ?? linha.DataLancamento.GetValueOrDefault();

        return tipoValorSufixoEParcela.Tipo.PagamentoFatura
            ? Lancamento.PagamentoFaturaCartao(
                data,
                hora: null,
                (ReaisPositivo)tipoValorSufixoEParcela.Valor.Value,
                (TextoObrigatorio)descricao,
                linha.Vendedor,
                categoriaBanco: null,
                identidade)
            : Lancamento.DoCartao(
                data,
                hora: null,
                tipoValorSufixoEParcela.Valor,
                (TextoObrigatorio)descricao,
                linha.Vendedor,
                tipoValorSufixoEParcela.Parcela,
                categoriaBanco: null,
                identidade);
    }

    /// <summary>
    /// Põe cada lançamento sem final (o pagamento da fatura) no menor final do arquivo. Falha quando nenhuma linha traz
    /// final. Sem lançamentos, devolve a lista vazia, e a <see cref="Fatura"/> recusa.
    /// </summary>
    private static Result<IReadOnlyList<LancamentoFatura>> PorNoMenorFinal(IReadOnlyList<LancamentoLido> lidos)
    {
        if (lidos.Count == 0)
            return Result.Success<IReadOnlyList<LancamentoFatura>>([]);

        SufixoCartao? menor = lidos.Select(l => l.Sufixo).OfType<SufixoCartao>().Order().FirstOrDefault();
        if (menor is null)
            return Result.Failure<IReadOnlyList<LancamentoFatura>>("O arquivo não traz o final de nenhum cartão.");

        return lidos.Select(l => LancamentoFatura.Criar(l.Lancamento, l.Sufixo ?? menor)).ToList();
    }

    private static FalhaLeituraArquivo ConteudoInvalido(string mensagem)
    {
        return FalhaLeituraArquivo.NoArquivo(TipoFalhaLeituraArquivo.ConteudoInvalido, mensagem);
    }

    [GeneratedRegex("^[0-9]{1,3}$")]
    private static partial Regex NumeroParcela();

    /// <summary>
    /// A coluna do valor, se o final do cartão é obrigatório, se há parcela e se a linha é o pagamento da fatura anterior,
    /// conforme o código do tipo de lançamento.
    /// </summary>
    internal sealed record TipoLancamento(bool Credito, bool FinalObrigatorio, bool Parcelado, bool PagamentoFatura);

    [CsvRecord]
    internal sealed partial class LinhaFaturaMercadoPago
    {
        private const string FormatoData = "dd/MM/yyyy";

        [CsvColumn("data_compra"), CsvFormat(FormatoData)]
        public DateOnly? DataCompra { get; init; }

        [CsvColumn("data_lancamento"), CsvFormat(FormatoData)]
        public DateOnly? DataLancamento { get; init; }

        [CsvColumn("tipo_lancamento")]
        public string TipoLancamento { get; init; } = string.Empty;

        [CsvColumn("descricao_fatura")]
        public string Descricao { get; init; } = string.Empty;

        [CsvColumn("vendedor")]
        public string Vendedor { get; init; } = string.Empty;

        [CsvColumn("numero_transacao")]
        public string NumeroTransacao { get; init; } = string.Empty;

        [CsvColumn("cartao_final")]
        public string CartaoFinal { get; init; } = string.Empty;

        [CsvColumn("parcela_atual")]
        public string ParcelaAtual { get; init; } = string.Empty;

        [CsvColumn("parcelas_totais")]
        public string ParcelasTotais { get; init; } = string.Empty;

        [CsvColumn("moeda")]
        public string Moeda { get; init; } = string.Empty;

        [CsvColumn("valor_cobrado_fatura"), CsvConverter(typeof(ValorFaturaConverter))]
        public decimal? ValorCobrado { get; init; }

        [CsvColumn("valor_creditado_fatura"), CsvConverter(typeof(ValorFaturaConverter))]
        public decimal? ValorCreditado { get; init; }
    }

    /// <summary>
    /// Valor no formato do prompt, ou vazio: sem sinal nem milhar e com 2 casas depois da vírgula. Até 15 dígitos
    /// inteiros, o mesmo teto do extrato: com mais, a soma das linhas de um arquivo poderia estourar o
    /// <see cref="decimal"/>.
    /// </summary>
    internal sealed partial class ValorFaturaConverter : ICsvConverter<decimal?>
    {
        // Próprio, e não a cultura pt-BR, que muda com o sistema e com a versão do ICU.
        private static readonly NumberFormatInfo _formato = new() { NumberDecimalSeparator = "," };

        public decimal? Read(CsvReader reader, int index)
        {
            string texto = reader.GetString(index);
            if (texto.Length == 0)
                return null;

            return Valor().IsMatch(texto)
                ? decimal.Parse(texto, NumberStyles.AllowDecimalPoint, _formato)
                : throw new FormatException("O valor não está no formato do prompt.");
        }

        public void Write(CsvWriter writer, decimal? value) => throw new NotSupportedException();

        [GeneratedRegex("^[0-9]{1,15},[0-9]{2}$")]
        private static partial Regex Valor();
    }
}
