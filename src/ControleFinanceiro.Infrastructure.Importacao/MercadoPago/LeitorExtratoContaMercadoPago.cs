using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LancamentoComSaldo = (ControleFinanceiro.Domain.Importacoes.LancamentoContabilizado Lancamento, ControleFinanceiro.Domain.Shared.ValueObjects.Reais SaldoParcial);
using SaldosArquivo = (ControleFinanceiro.Domain.Shared.ValueObjects.Reais Inicial, ControleFinanceiro.Domain.Shared.ValueObjects.Reais Final);

namespace ControleFinanceiro.Infrastructure.Importacao.MercadoPago;

/// <summary>
/// Lê o extrato CSV da conta do Mercado Pago: o resumo dos saldos nas 2 primeiras linhas, uma linha em branco e
/// depois as colunas das transações, separadas por <c>;</c>, com o saldo parcial depois de cada uma.
/// </summary>
/// <remarks>
/// <para>
/// O arquivo não traz a conta nem o período: o sufixo vem do usuário, e o período vai da primeira à última data.
/// </para>
/// <para>
/// O saldo inicial vira o saldo da véspera do primeiro dia, então o <see cref="Extrato"/> confere também o primeiro
/// dia. O saldo de cada dia é o saldo parcial da última linha dele, e o saldo final tem de ser o da última linha.
/// Os créditos e os débitos do resumo só têm o formato conferido: uma linha perdida já quebra o saldo do dia.
/// </para>
/// <para>
/// O lançamento é reconhecido numa reimportação só pelo <c>REFERENCE_ID</c> (<see cref="IdentidadeLancamento.Unica"/>).
/// Um id que voltasse em outro arquivo, como uma devolução com o id da compra, seria tomado pela transação já gravada,
/// e só o aviso da soma do período apontaria a diferença. Nenhum arquivo conferido repete o id.
/// </para>
/// <para>
/// O CPF e o CNPJ que vêm na descrição, colados à contraparte, saem antes de a descrição deixar o leitor. As
/// mensagens de falha citam só o número da linha, e a <see cref="CsvHelperException"/>, que traz o registro cru, é
/// capturada.
/// </para>
/// </remarks>
public sealed partial class LeitorExtratoContaMercadoPago : ILeitorExtratoContaInformada
{
    private const int LinhasAntesColunas = 3;
    private const string ColunasResumo = "INITIAL_BALANCE;CREDITS;DEBITS;FINAL_BALANCE";
    private const char Separador = ';';

    /// <summary>Descrições que trazem a contraparte depois do prefixo.</summary>
    private static readonly string[] _prefixosComContraparte =
        ["Pix enviado", "Pix recebido", "Pagamento com QR Pix", "Pagamento de conta", "Pagamento de assinatura"];

    private static readonly CsvConfiguration _configuracao = new(CultureInfo.InvariantCulture)
    {
        Delimiter = Separador.ToString(),
        DetectColumnCountChanges = true,
    };

    public LayoutArquivoImportacao Layout => LayoutArquivoImportacao.MercadoPagoContaCsv;

    /// <summary>Lê o resumo, tira dele os saldos inicial e final, e depois lê as transações e monta o extrato.</summary>
    public async Task<Result<Extrato>> LerAsync(Stream arquivo, SufixoConta sufixoConta, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        return await LerResumoAsync(leitor, cancellationToken)
            .Bind(LerSaldos)
            .Bind(saldos => LerExtratoAsync(leitor, sufixoConta, saldos, cancellationToken));
    }

    private static async Task<Result<IReadOnlyList<string>>> LerResumoAsync(StreamReader leitor, CancellationToken cancellationToken)
    {
        List<string> linhas = [];
        for (int i = 0; i < LinhasAntesColunas; i++)
        {
            string? linha = await leitor.ReadLineAsync(cancellationToken);
            if (linha is null)
                return Result.Failure<IReadOnlyList<string>>("O arquivo termina antes das colunas do extrato da conta do Mercado Pago.");

            linhas.Add(linha);
        }

        return linhas;
    }

    /// <param name="resumo">As 3 linhas antes das colunas: os nomes, os valores e a linha em branco.</param>
    private static Result<SaldosArquivo> LerSaldos(IReadOnlyList<string> resumo)
    {
        string[] valores = resumo[1].Split(Separador);

        return Result
            .FailureIf(resumo[0] != ColunasResumo, "A linha 1 do arquivo não traz as colunas do resumo do extrato da conta do Mercado Pago.")
            .Ensure(
                () => valores.Length == ColunasResumo.Split(Separador).Length && valores.All(ValorMercadoPagoConverter.EhValor),
                "A linha 2 do arquivo não está no layout do resumo do extrato da conta do Mercado Pago.")
            .Ensure(() => resumo[2].Length == 0, "A linha 3 do arquivo deveria estar em branco no extrato da conta do Mercado Pago.")
            .Map(() => ((Reais)ValorMercadoPagoConverter.Ler(valores[0]), (Reais)ValorMercadoPagoConverter.Ler(valores[3])));
    }

    private static async Task<Result<Extrato>> LerExtratoAsync(
        StreamReader leitor,
        SufixoConta sufixoConta,
        SaldosArquivo saldos,
        CancellationToken cancellationToken)
    {
        return await LerLancamentosAsync(leitor, cancellationToken)
            .Ensure(lidos => lidos.Count > 0, "O arquivo não traz transações.")
            .Ensure(lidos => lidos[^1].SaldoParcial == saldos.Final, "O saldo final do arquivo não fecha com o saldo da última transação.")
            .Bind(lidos => MontarExtrato(sufixoConta, saldos.Inicial, lidos));
    }

    /// <param name="lidos">Pelo menos uma transação, na ordem do arquivo.</param>
    private static Result<Extrato> MontarExtrato(SufixoConta sufixoConta, Reais saldoInicial, IReadOnlyList<LancamentoComSaldo> lidos)
    {
        List<DateOnly> datas = lidos.Select(l => l.Lancamento.DataContabil).ToList();
        SaldoDiario saldoVespera = SaldoDiario.Criar(datas.Min().AddDays(-1), saldoInicial);

        // O saldo parcial da última linha de cada dia é o saldo do dia.
        IEnumerable<SaldoDiario> saldosDias = lidos
            .GroupBy(l => l.Lancamento.DataContabil)
            .Select(dia => SaldoDiario.Criar(dia.Key, dia.Last().SaldoParcial));

        return PeriodoArquivo
            .Criar(datas.Min(), datas.Max())
            .Bind(periodo => Extrato.Criar(
                Banco.MercadoPago,
                TipoConta.ContaPagamento,
                sufixoConta,
                periodo,
                lidos.Select(l => l.Lancamento),
                saldosDias.Prepend(saldoVespera)));
    }

    /// <summary>
    /// Lê as colunas e depois cada transação, com o saldo parcial depois dela. Para na primeira linha que não está no
    /// layout.
    /// </summary>
    /// <remarks>O CSV fecha o <paramref name="leitor"/> ao terminar.</remarks>
    private static async Task<Result<IReadOnlyList<LancamentoComSaldo>>> LerLancamentosAsync(
        StreamReader leitor,
        CancellationToken cancellationToken)
    {
        using CsvReader csv = new(leitor, _configuracao);
        csv.Context.RegisterClassMap<LinhaExtratoMercadoPagoMap>();

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken));
    }

    private static async Task<Result> LerColunasAsync(CsvReader csv)
    {
        try
        {
            if (!await csv.ReadAsync())
                return Result.Failure("O arquivo termina antes das colunas do extrato da conta do Mercado Pago.");

            csv.ReadHeader();
            csv.ValidateHeader<LinhaExtratoMercadoPago>();
            return Result.Success();
        }
        catch (CsvHelperException)
        {
            return Result.Failure($"A linha {LinhasAntesColunas + 1} do arquivo não traz as colunas do extrato da conta do Mercado Pago.");
        }
    }

    private static async Task<Result<IReadOnlyList<LancamentoComSaldo>>> LerLinhasAsync(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        List<LancamentoComSaldo> lidos = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Result<Maybe<LinhaExtratoMercadoPago>> proxima = await LerProximaLinhaAsync(csv);
            if (proxima.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoComSaldo>>(proxima.Error);

            if (proxima.Value.HasNoValue)
                return lidos;

            LinhaExtratoMercadoPago linha = proxima.Value.Value;
            Result<LancamentoContabilizado> lancamento = Converter(linha);
            if (lancamento.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoComSaldo>>($"A linha {NumeroLinha(csv)} do arquivo {lancamento.Error}");

            lidos.Add((lancamento.Value, linha.SaldoParcial));
        }
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaExtratoMercadoPago>>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaExtratoMercadoPago>() : Maybe<LinhaExtratoMercadoPago>.None;
        }
        catch (CsvHelperException)
        {
            return Result.Failure<Maybe<LinhaExtratoMercadoPago>>(
                $"A linha {NumeroLinha(csv)} do arquivo não está no layout do extrato da conta do Mercado Pago.");
        }
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoContabilizado> Converter(LinhaExtratoMercadoPago linha)
    {
        string descricao = TirarDocumentos(linha.Descricao);

        return Result
            .FailureIf(descricao.Length == 0, "não tem descrição.")
            .Bind(() => IdentidadeLancamento
                .Unica(linha.IdReferencia)
                .MapError(_ => "não tem o identificador da transação."))
            .Map(identidade => Contabilizar(linha, descricao, identidade));
    }

    /// <param name="descricao">Sem CPF nem CNPJ, aparada e não vazia.</param>
    private static LancamentoContabilizado Contabilizar(LinhaExtratoMercadoPago linha, string descricao, IdentidadeLancamento identidade)
    {
        (TextoObrigatorio descricaoSemContraparte, string? contraparte) = DescricaoEContraparte(descricao);

        Lancamento lancamento = Lancamento.Criar(
            linha.Data, hora: null, linha.Valor, descricaoSemContraparte, contraparte, parcela: null, categoriaBanco: null, identidade);

        // O Mercado Pago não separa a data contábil: o saldo parcial já é o do dia da transação.
        return LancamentoContabilizado.Criar(lancamento, linha.Data);
    }

    /// <summary>
    /// Nas descrições com um prefixo conhecido, o que vem depois dele é a contraparte. As outras ficam inteiras, sem
    /// contraparte.
    /// </summary>
    /// <param name="descricao">Sem CPF nem CNPJ, aparada e não vazia.</param>
    private static (TextoObrigatorio Descricao, string? Contraparte) DescricaoEContraparte(string descricao)
    {
        string? prefixo = Array.Find(
            _prefixosComContraparte,
            p => descricao.StartsWith(p, StringComparison.OrdinalIgnoreCase) && (descricao.Length == p.Length || descricao[p.Length] == ' '));

        return prefixo is null
            ? ((TextoObrigatorio)descricao, null)
            : ((TextoObrigatorio)descricao[..prefixo.Length], descricao[prefixo.Length..]);
    }

    /// <summary>
    /// Tira cada CNPJ e cada CPF válidos, com ou sem máscara, e cada CPF mascarado com <c>*</c>, junto com os espaços
    /// antes deles, e apara o que sobra. O CPF e o CNPJ só de dígitos saem mesmo colados a letras; o CNPJ com letras,
    /// só separado delas.
    /// </summary>
    private static string TirarDocumentos(string texto)
    {
        string semCnpjNumerico = CandidatoCnpjNumerico().Replace(texto, candidato => TirarSe(candidato, numero => Cnpj.Criar(numero).IsSuccess));
        string semCnpj = CandidatoCnpj().Replace(semCnpjNumerico, candidato => TirarSe(candidato, numero => Cnpj.Criar(numero).IsSuccess));
        string semCpf = CandidatoCpf().Replace(semCnpj, candidato => TirarSe(candidato, numero => Cpf.Criar(numero).IsSuccess));

        return CandidatoCpfMascarado()
            .Replace(semCpf, candidato => TirarSe(candidato, numero => numero.Contains('*')))
            .Trim();
    }

    /// <returns>Vazio quando o documento do <paramref name="candidato"/> é tirado; senão, o candidato como veio.</returns>
    private static string TirarSe(Match candidato, Func<string, bool> tirar)
    {
        return tirar(candidato.Groups["documento"].Value) ? string.Empty : candidato.Value;
    }

    private static long NumeroLinha(CsvReader csv) => LinhasAntesColunas + csv.Parser.RawRow;

    // [0-9], e não \d, que aceita dígitos de outros alfabetos. Os separadores entre os grupos são opcionais.
    [GeneratedRegex(@"\s*(?<![0-9])(?<documento>[0-9]{3}[. ]?[0-9]{3}[. ]?[0-9]{3}[-. ]?[0-9]{2})(?![0-9])")]
    private static partial Regex CandidatoCpf();

    [GeneratedRegex(@"\s*(?<![0-9*])(?<documento>[0-9*]{3}\.?[0-9*]{3}\.?[0-9*]{3}-?[0-9*]{2})(?![0-9*])")]
    private static partial Regex CandidatoCpfMascarado();

    [GeneratedRegex(@"\s*(?<![0-9])(?<documento>[0-9]{2}[. ]?[0-9]{3}[. ]?[0-9]{3}[/ ]?[0-9]{4}[- ]?[0-9]{2})(?![0-9])")]
    private static partial Regex CandidatoCnpjNumerico();

    // As 12 primeiras posições do CNPJ podem ter letras; os verificadores são sempre dígitos. Sem espaço entre os grupos:
    // com letras, ele faria trechos de nomes virarem candidatos.
    [GeneratedRegex(@"\s*(?<![0-9A-Za-z])(?<documento>[0-9A-Za-z]{2}\.?[0-9A-Za-z]{3}\.?[0-9A-Za-z]{3}/?[0-9A-Za-z]{4}-?[0-9]{2})(?![0-9A-Za-z])")]
    private static partial Regex CandidatoCnpj();

    private sealed class LinhaExtratoMercadoPago
    {
        public DateOnly Data { get; init; }
        public string Descricao { get; init; } = string.Empty;
        public string IdReferencia { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public decimal SaldoParcial { get; init; }
    }

    private sealed class LinhaExtratoMercadoPagoMap : ClassMap<LinhaExtratoMercadoPago>
    {
        private const string FormatoData = "dd-MM-yyyy";

        public LinhaExtratoMercadoPagoMap()
        {
            Map(l => l.Data).Name("RELEASE_DATE").TypeConverterOption.Format(FormatoData);
            Map(l => l.Descricao).Name("TRANSACTION_TYPE");
            Map(l => l.IdReferencia).Name("REFERENCE_ID");
            Map(l => l.Valor).Name("TRANSACTION_NET_AMOUNT").TypeConverter<ValorMercadoPagoConverter>();
            Map(l => l.SaldoParcial).Name("PARTIAL_BALANCE").TypeConverter<ValorMercadoPagoConverter>();
        }
    }

    /// <summary>
    /// Valor no formato do Mercado Pago: sinal na frente, milhar com ponto e 2 casas depois da vírgula. O formato é
    /// conferido antes, porque o <see cref="NumberStyles.AllowThousands"/> aceita o ponto fora do lugar do milhar. Até
    /// 4 grupos de milhar (15 dígitos inteiros): com mais, a soma das linhas de um arquivo poderia estourar o
    /// <see cref="decimal"/>.
    /// </summary>
    private sealed partial class ValorMercadoPagoConverter : DefaultTypeConverter
    {
        private const NumberStyles Estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint;

        // Próprio, e não a cultura pt-BR, que muda com o sistema e com a versão do ICU.
        private static readonly NumberFormatInfo _formato = new()
        {
            NegativeSign = "-",
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
        };

        public static bool EhValor(string texto) => Valor().IsMatch(texto);

        /// <param name="texto">Já conferido com o <see cref="EhValor"/>.</param>
        public static decimal Ler(string texto) => decimal.Parse(texto, Estilo, _formato);

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            return text is not null && EhValor(text) ? Ler(text) : base.ConvertFromString(text, row, memberMapData);
        }

        [GeneratedRegex(@"^-?[0-9]{1,3}(\.[0-9]{3}){0,4},[0-9]{2}$")]
        private static partial Regex Valor();
    }
}
