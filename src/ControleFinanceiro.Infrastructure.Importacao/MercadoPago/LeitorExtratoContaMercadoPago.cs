using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
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
/// O CPF e o CNPJ que vêm na descrição, colados à contraparte, ficam: quem os tira é a saída para o modelo. As
/// mensagens de falha citam só o número da linha, e a <see cref="CsvFormatException"/>, que pode trazer dados do registro, é
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

    private static readonly CsvReaderOptions _opcoes = OpcoesLeituraCsv.Criar(Separador);

    /// <summary>As colunas mapeadas, com a grafia exata: o CsvHelper aceitaria outra caixa e espaços em volta.</summary>
    private static readonly string[] _colunas =
        ["RELEASE_DATE", "TRANSACTION_TYPE", "REFERENCE_ID", "TRANSACTION_NET_AMOUNT", "PARTIAL_BALANCE"];

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
        using CsvReader csv = CsvReader.Create(leitor, _opcoes);

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken));
    }

    private static async Task<Result> LerColunasAsync(CsvReader csv)
    {
        try
        {
            await csv.ReadHeaderAsync();
            if (csv.Headers.Count == 0)
                return Result.Failure("O arquivo termina antes das colunas do extrato da conta do Mercado Pago.");

            return Result.FailureIf(
                !_colunas.All(csv.Headers.Contains),
                $"A linha {LinhasAntesColunas + 1} do arquivo não traz as colunas do extrato da conta do Mercado Pago.");
        }
        catch (CsvFormatException)
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
        catch (CsvFormatException erro)
        {
            return Result.Failure<Maybe<LinhaExtratoMercadoPago>>(
                $"A linha {LinhasAntesColunas + erro.LineNumber} do arquivo não está no layout do extrato da conta do Mercado Pago.");
        }
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoContabilizado> Converter(LinhaExtratoMercadoPago linha)
    {
        string descricao = linha.Descricao.Trim();

        return Result
            .FailureIf(descricao.Length == 0, "não tem descrição.")
            .Bind(() => IdentidadeLancamento
                .Unica(linha.IdReferencia)
                .MapError(_ => "não tem o identificador da transação."))
            .Map(identidade => Contabilizar(linha, descricao, identidade));
    }

    /// <param name="descricao">Aparada e não vazia.</param>
    private static LancamentoContabilizado Contabilizar(LinhaExtratoMercadoPago linha, string descricao, IdentidadeLancamento identidade)
    {
        (TextoObrigatorio descricaoSemContraparte, string? contraparte) = DescricaoEContraparte(descricao);

        Lancamento lancamento = Lancamento.DaConta(
            linha.Data, hora: null, linha.Valor, descricaoSemContraparte, contraparte, categoriaBanco: null, identidade);

        // O Mercado Pago não separa a data contábil: o saldo parcial já é o do dia da transação.
        return LancamentoContabilizado.Criar(lancamento, linha.Data);
    }

    /// <summary>
    /// Nas descrições com um prefixo conhecido, o que vem depois dele é a contraparte. As outras ficam inteiras, sem
    /// contraparte.
    /// </summary>
    /// <param name="descricao">Aparada e não vazia.</param>
    private static (TextoObrigatorio Descricao, string? Contraparte) DescricaoEContraparte(string descricao)
    {
        string? prefixo = Array.Find(
            _prefixosComContraparte,
            p => descricao.StartsWith(p, StringComparison.OrdinalIgnoreCase) && (descricao.Length == p.Length || descricao[p.Length] == ' '));

        return prefixo is null
            ? ((TextoObrigatorio)descricao, null)
            : ((TextoObrigatorio)descricao[..prefixo.Length], descricao[prefixo.Length..]);
    }

    private static long NumeroLinha(CsvReader csv) => LinhasAntesColunas + csv.LineNumber;

    [CsvRecord]
    internal sealed partial class LinhaExtratoMercadoPago
    {
        private const string FormatoData = "dd-MM-yyyy";

        [CsvColumn("RELEASE_DATE"), CsvFormat(FormatoData)]
        public DateOnly Data { get; init; }

        [CsvColumn("TRANSACTION_TYPE")]
        public string Descricao { get; init; } = string.Empty;

        [CsvColumn("REFERENCE_ID")]
        public string IdReferencia { get; init; } = string.Empty;

        [CsvColumn("TRANSACTION_NET_AMOUNT"), CsvConverter(typeof(ValorMercadoPagoConverter))]
        public decimal Valor { get; init; }

        [CsvColumn("PARTIAL_BALANCE"), CsvConverter(typeof(ValorMercadoPagoConverter))]
        public decimal SaldoParcial { get; init; }
    }

    /// <summary>
    /// Valor no formato do Mercado Pago: sinal na frente, milhar com ponto e 2 casas depois da vírgula. O formato é
    /// conferido antes, porque o <see cref="NumberStyles.AllowThousands"/> aceita o ponto fora do lugar do milhar. Até
    /// 4 grupos de milhar (15 dígitos inteiros): com mais, a soma das linhas de um arquivo poderia estourar o
    /// <see cref="decimal"/>.
    /// </summary>
    internal sealed partial class ValorMercadoPagoConverter : ICsvConverter<decimal>
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

        public decimal Read(CsvReader reader, int index)
        {
            string texto = reader.GetString(index);
            return EhValor(texto) ? Ler(texto) : throw new FormatException("O valor não está no formato do Mercado Pago.");
        }

        public void Write(CsvWriter writer, decimal value) => throw new NotSupportedException();

        [GeneratedRegex(@"^-?[0-9]{1,3}(\.[0-9]{3}){0,4},[0-9]{2}$")]
        private static partial Regex Valor();
    }
}
