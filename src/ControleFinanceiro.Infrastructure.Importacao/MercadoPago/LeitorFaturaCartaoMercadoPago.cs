using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LancamentoLido = (ControleFinanceiro.Domain.Transacoes.Lancamento Lancamento, ControleFinanceiro.Domain.Cartoes.SufixoCartao? Sufixo);
using TipoEValor = (ControleFinanceiro.Infrastructure.Importacao.MercadoPago.LeitorFaturaCartaoMercadoPago.TipoLancamento Tipo, ControleFinanceiro.Domain.Shared.ValueObjects.Reais Valor);
using ValorSufixoEParcela = (ControleFinanceiro.Domain.Shared.ValueObjects.Reais Valor, ControleFinanceiro.Domain.Cartoes.SufixoCartao? Sufixo, ControleFinanceiro.Domain.Transacoes.Parcela? Parcela);

namespace ControleFinanceiro.Infrastructure.Importacao.MercadoPago;

/// <summary>
/// Lê a fatura CSV do cartão do Mercado Pago, que o Mago gera com o prompt de <c>docs/fatura-mercado-pago.md</c>: as
/// 19 colunas do prompt na primeira linha, separadas por <c>;</c>, e depois um lançamento por linha.
/// </summary>
/// <remarks>
/// <para>
/// Como o arquivo é gerado por IA, o leitor não dá palpite: as colunas têm de ser as do prompt, na ordem, e o tipo de
/// lançamento tem de ser um dos conhecidos, que diz a coluna do valor e se o final do cartão é obrigatório. Um tipo
/// novo entra na lista quando aparecer num arquivo.
/// </para>
/// <para>
/// O lançamento é reconhecido numa reimportação pelo <c>numero_transacao</c>, pela parcela e pelo valor
/// (<see cref="IdentidadeLancamento.Composta(string)"/>): a parcela seguinte da mesma compra, na fatura seguinte,
/// traz o mesmo número.
/// </para>
/// <para>
/// O pagamento da fatura vem sem final e vai para o menor final do arquivo, para que a escolha não dependa da ordem
/// das linhas.
/// </para>
/// <para>
/// O CPF e o CNPJ da descrição e do vendedor ficam: quem os tira é a saída para o modelo. Os produtos e o número do
/// pedido não são mapeados, nem o mês de referência, o vencimento, o fechamento, o valor original da compra e o
/// status. As mensagens de falha citam só o número da linha, e a <see cref="CsvHelperException"/>, que traz o registro
/// cru, é capturada.
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
        ["compra_a_vista"] = new(Credito: false, FinalObrigatorio: true, Parcelado: false),
        ["compra_parcelada"] = new(Credito: false, FinalObrigatorio: true, Parcelado: true),
        ["pagamento_debito_automatico_pos_fechamento"] = new(Credito: true, FinalObrigatorio: false, Parcelado: false),
    };

    private static readonly CsvConfiguration _configuracao = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        DetectColumnCountChanges = true,
    };

    public LayoutArquivoImportacao Layout => LayoutArquivoImportacao.MercadoPagoCartaoCsv;

    /// <summary>Confere as colunas, lê cada lançamento, põe o pagamento num cartão e monta a fatura.</summary>
    public async Task<Result<Fatura>> LerAsync(Stream arquivo, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using CsvReader csv = new(leitor, _configuracao);
        csv.Context.RegisterClassMap<LinhaFaturaMercadoPagoMap>();

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken))
            .Bind(PorNoMenorFinal)
            .Bind(lancamentos => Fatura.Criar(Banco.MercadoPago, lancamentos));
    }

    /// <summary>
    /// As colunas têm de ser exatamente as do prompt, na ordem: o <c>ValidateHeader</c> do CsvHelper aceitaria colunas
    /// a mais e em outra ordem.
    /// </summary>
    private static async Task<Result> LerColunasAsync(CsvReader csv)
    {
        try
        {
            if (!await csv.ReadAsync())
                return Result.Failure("O arquivo termina antes das colunas da fatura do cartão do Mercado Pago.");

            csv.ReadHeader();
            return Result.FailureIf(
                csv.HeaderRecord is null || !csv.HeaderRecord.SequenceEqual(_colunas, StringComparer.Ordinal),
                "A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
        }
        catch (CsvHelperException)
        {
            return Result.Failure(
                "A linha 1 do arquivo não traz as colunas do prompt da fatura do cartão do Mercado Pago (docs/fatura-mercado-pago.md).");
        }
    }

    /// <summary>Para na primeira linha que não está no layout.</summary>
    private static async Task<Result<IReadOnlyList<LancamentoLido>>> LerLinhasAsync(CsvReader csv, CancellationToken cancellationToken)
    {
        List<LancamentoLido> lidos = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Result<Maybe<LancamentoLido>> proximo = await LerProximaLinhaAsync(csv)
                .Bind(linha => ConverterSeHouver(csv, linha));

            if (proximo.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoLido>>(proximo.Error);

            if (proximo.Value.HasNoValue)
                return lidos;

            Maybe<LancamentoLido> lancamentoLido = proximo.Value;
            lidos.Add(lancamentoLido.Value);
        }
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaFaturaMercadoPago>>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaFaturaMercadoPago>() : Maybe<LinhaFaturaMercadoPago>.None;
        }
        catch (CsvHelperException)
        {
            return Result.Failure<Maybe<LinhaFaturaMercadoPago>>(
                $"A linha {csv.Parser.RawRow} do arquivo não está no layout da fatura do cartão do Mercado Pago.");
        }
    }

    /// <param name="linha">Vazia no fim do arquivo.</param>
    private static Result<Maybe<LancamentoLido>> ConverterSeHouver(CsvReader csv, Maybe<LinhaFaturaMercadoPago> linha)
    {
        if (linha.HasNoValue)
            return Maybe<LancamentoLido>.None;

        return Converter(linha.Value)
            .Map(Maybe.From)
            .MapError(erro => $"A linha {csv.Parser.RawRow} do arquivo {erro}");
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

    private static Result<ValorSufixoEParcela> LerSufixoEParcela(LinhaFaturaMercadoPago linha, TipoEValor tipoEValor)
    {
        return LerSufixo(linha.CartaoFinal, tipoEValor.Tipo)
            .Bind(sufixo => LerParcela(linha, tipoEValor.Tipo).Map(parcela => (tipoEValor.Valor, sufixo, parcela)));
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
    private static Result<LancamentoLido> Lancar(LinhaFaturaMercadoPago linha, string descricao, ValorSufixoEParcela valorSufixoEParcela)
    {
        return IdentidadeLancamento
            .Composta(linha.NumeroTransacao)
            .MapError(_ => "não tem o número da transação.")
            .Map(identidade => identidade.ComParcela(valorSufixoEParcela.Parcela).ComValor(valorSufixoEParcela.Valor))
            .Map(identidade => Lancamento.Criar(
                linha.DataCompra ?? linha.DataLancamento.GetValueOrDefault(),
                hora: null,
                valorSufixoEParcela.Valor,
                (TextoObrigatorio)descricao,
                linha.Vendedor,
                valorSufixoEParcela.Parcela,
                categoriaBanco: null,
                identidade))
            .Map(lancamento => (lancamento, valorSufixoEParcela.Sufixo));
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

    [GeneratedRegex("^[0-9]{1,3}$")]
    private static partial Regex NumeroParcela();

    /// <summary>A coluna do valor e se o final do cartão é obrigatório, conforme o código do tipo de lançamento.</summary>
    internal sealed record TipoLancamento(bool Credito, bool FinalObrigatorio, bool Parcelado);

    private sealed class LinhaFaturaMercadoPago
    {
        public DateOnly? DataCompra { get; init; }
        public DateOnly? DataLancamento { get; init; }
        public string TipoLancamento { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string Vendedor { get; init; } = string.Empty;
        public string NumeroTransacao { get; init; } = string.Empty;
        public string CartaoFinal { get; init; } = string.Empty;
        public string ParcelaAtual { get; init; } = string.Empty;
        public string ParcelasTotais { get; init; } = string.Empty;
        public string Moeda { get; init; } = string.Empty;
        public decimal? ValorCobrado { get; init; }
        public decimal? ValorCreditado { get; init; }
    }

    private sealed class LinhaFaturaMercadoPagoMap : ClassMap<LinhaFaturaMercadoPago>
    {
        private const string FormatoData = "dd/MM/yyyy";

        public LinhaFaturaMercadoPagoMap()
        {
            Map(l => l.DataCompra).Name("data_compra").TypeConverterOption.Format(FormatoData);
            Map(l => l.DataLancamento).Name("data_lancamento").TypeConverterOption.Format(FormatoData);
            Map(l => l.TipoLancamento).Name("tipo_lancamento");
            Map(l => l.Descricao).Name("descricao_fatura");
            Map(l => l.Vendedor).Name("vendedor");
            Map(l => l.NumeroTransacao).Name("numero_transacao");
            Map(l => l.CartaoFinal).Name("cartao_final");
            Map(l => l.ParcelaAtual).Name("parcela_atual");
            Map(l => l.ParcelasTotais).Name("parcelas_totais");
            Map(l => l.Moeda).Name("moeda");
            Map(l => l.ValorCobrado).Name("valor_cobrado_fatura").TypeConverter<ValorFaturaConverter>();
            Map(l => l.ValorCreditado).Name("valor_creditado_fatura").TypeConverter<ValorFaturaConverter>();
        }
    }

    /// <summary>
    /// Valor no formato do prompt, ou vazio: sem sinal nem milhar e com 2 casas depois da vírgula. Até 15 dígitos
    /// inteiros, o mesmo teto do extrato: com mais, a soma das linhas de um arquivo poderia estourar o
    /// <see cref="decimal"/>.
    /// </summary>
    private sealed partial class ValorFaturaConverter : DefaultTypeConverter
    {
        // Próprio, e não a cultura pt-BR, que muda com o sistema e com a versão do ICU.
        private static readonly NumberFormatInfo _formato = new() { NumberDecimalSeparator = "," };

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            return Valor().IsMatch(text)
                ? decimal.Parse(text, NumberStyles.AllowDecimalPoint, _formato)
                : base.ConvertFromString(text, row, memberMapData);
        }

        [GeneratedRegex("^[0-9]{1,15},[0-9]{2}$")]
        private static partial Regex Valor();
    }
}
