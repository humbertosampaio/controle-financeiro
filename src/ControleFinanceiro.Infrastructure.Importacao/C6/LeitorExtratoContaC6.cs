using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ContaEPeriodo = (ControleFinanceiro.Domain.Contas.SufixoConta Sufixo, ControleFinanceiro.Domain.Importacoes.PeriodoArquivo Periodo);
using DatasDoPeriodo = (System.DateOnly Inicio, System.DateOnly Fim);
using LancamentoComSaldo = (ControleFinanceiro.Domain.Importacoes.LancamentoContabilizado Lancamento, ControleFinanceiro.Domain.Importacoes.SaldoDiario Saldo);

namespace ControleFinanceiro.Infrastructure.Importacao.C6;

/// <summary>
/// Lê o extrato CSV da conta corrente do C6: 8 linhas de cabeçalho livre, com a conta e o período, e
/// depois as colunas dos lançamentos.
/// </summary>
/// <remarks>
/// <para>
/// Da conta, só o sufixo sai daqui: o número completo e a agência ficam em variável local.
/// </para>
/// <para>
/// As mensagens de falha citam só o número da linha. A <see cref="CsvHelperException"/> traz o registro
/// cru, então ela é capturada e trocada por uma mensagem própria.
/// </para>
/// </remarks>
public sealed partial class LeitorExtratoContaC6 : ILeitorExtrato
{
    private const int LinhasAntesDasColunas = 8;
    private const string SeparadorDescricao = " · ";

    /// <summary>Títulos de Pix que trazem o nome da contraparte depois do prefixo.</summary>
    private static readonly string[] _prefixosPixComContraparte = ["Pix recebido de", "Pix enviado para", "Devol recebida pix de"];

    private static readonly CsvConfiguration _configuracao = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        DetectColumnCountChanges = true,
    };

    public LayoutArquivoImportacao Layout => LayoutArquivoImportacao.C6ContaCsv;

    /// <summary>
    /// Lê o cabeçalho, tira dele a conta e o período, e depois lê os lançamentos e monta o extrato.
    /// </summary>
    public async Task<Result<Extrato>> LerAsync(Stream arquivo, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        return await LerCabecalhoAsync(leitor, cancellationToken)
            .Bind(LerContaEPeriodo)
            .Bind(contaEPeriodo => LerExtratoAsync(leitor, contaEPeriodo.Sufixo, contaEPeriodo.Periodo, cancellationToken));
    }

    private static async Task<Result<IReadOnlyList<string>>> LerCabecalhoAsync(StreamReader leitor, CancellationToken cancellationToken)
    {
        List<string> linhas = [];
        for (int i = 0; i < LinhasAntesDasColunas; i++)
        {
            string? linha = await leitor.ReadLineAsync(cancellationToken);
            if (linha is null)
                return Result.Failure<IReadOnlyList<string>>("O arquivo termina antes das colunas do extrato da conta do C6.");

            linhas.Add(linha);
        }

        return linhas;
    }

    private static Result<ContaEPeriodo> LerContaEPeriodo(IReadOnlyList<string> cabecalho)
    {
        return LerSufixoConta(cabecalho)
            .Bind(sufixo => LerPeriodo(cabecalho).Map(periodo => (sufixo, periodo)));
    }

    private static Result<SufixoConta> LerSufixoConta(IEnumerable<string> cabecalho)
    {
        return cabecalho
            .Select(l => NumeroConta().Match(l))
            .TryFirst(m => m.Success)
            .Map(m => m.Groups["numero"].Value)
            .ToResult("O cabeçalho do arquivo não traz a conta do extrato.")
            // O C6 escreve o número com o dígito verificador colado. Em outro formato (o dígito separado por
            // hífen, por exemplo), os 4 últimos caracteres não seriam o sufixo: melhor falhar que associar outra conta.
            .Ensure(numero => NumeroContaComDigitoColado().IsMatch(numero), "O número da conta no cabeçalho do arquivo não está no formato do C6.")
            .Ensure(numero => numero.Length >= SufixoConta.QuantidadeCaracteres, "O número da conta no cabeçalho do arquivo é curto demais.")
            .Bind(numero => SufixoConta.Criar(numero[^SufixoConta.QuantidadeCaracteres..]));
    }

    private static Result<PeriodoArquivo> LerPeriodo(IEnumerable<string> cabecalho)
    {
        return cabecalho
            .Select(l => Periodo().Match(l))
            .TryFirst(m => m.Success)
            .ToResult("O cabeçalho do arquivo não traz o período do extrato.")
            .Bind(LerDatasDoPeriodo)
            .Bind(datas => PeriodoArquivo.Criar(datas.Inicio, datas.Fim));
    }

    private static Result<DatasDoPeriodo> LerDatasDoPeriodo(Match periodo)
    {
        return LerDataDoPeriodo(periodo.Groups["inicio"].Value)
            .Bind(inicio => LerDataDoPeriodo(periodo.Groups["fim"].Value).Map(fim => (inicio, fim)));
    }

    private static Result<DateOnly> LerDataDoPeriodo(string texto)
    {
        return Result.SuccessIf(
            DateOnly.TryParseExact(texto, LinhaExtratoC6Map.FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data),
            data,
            "O período no cabeçalho do arquivo não tem datas válidas.");
    }

    private static async Task<Result<Extrato>> LerExtratoAsync(
        StreamReader leitor,
        SufixoConta sufixo,
        PeriodoArquivo periodo,
        CancellationToken cancellationToken)
    {
        // Um saldo por dia contábil: o C6 repete o saldo em toda linha do dia. Se as linhas de um dia trouxerem
        // saldos diferentes, os dois ficam, e o extrato não nasce, com a falha apontando o dia.
        return await LerLancamentosAsync(leitor, cancellationToken)
            .Bind(lidos => Extrato.Criar(
                Banco.C6,
                TipoConta.ContaCorrente,
                sufixo,
                periodo,
                lidos.Select(l => l.Lancamento),
                lidos.Select(l => l.Saldo).Distinct()));
    }

    /// <summary>
    /// Lê as colunas e depois cada lançamento, com o saldo do dia contábil dele. Para na primeira linha que
    /// não está no layout.
    /// </summary>
    /// <remarks>O CSV fecha o <paramref name="leitor"/> ao terminar.</remarks>
    private static async Task<Result<IReadOnlyList<LancamentoComSaldo>>> LerLancamentosAsync(
        StreamReader leitor,
        CancellationToken cancellationToken)
    {
        using CsvReader csv = new(leitor, _configuracao);
        csv.Context.RegisterClassMap<LinhaExtratoC6Map>();

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken));
    }

    private static async Task<Result> LerColunasAsync(CsvReader csv)
    {
        try
        {
            if (!await csv.ReadAsync())
                return Result.Failure("O arquivo termina antes das colunas do extrato da conta do C6.");

            csv.ReadHeader();
            csv.ValidateHeader<LinhaExtratoC6>();
            return Result.Success();
        }
        catch (CsvHelperException)
        {
            return Result.Failure($"A linha {LinhasAntesDasColunas + 1} do arquivo não traz as colunas do extrato da conta do C6.");
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

            Result<Maybe<LinhaExtratoC6>> proxima = await LerProximaLinhaAsync(csv);
            if (proxima.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoComSaldo>>(proxima.Error);

            if (proxima.Value.HasNoValue)
                return lidos;

            LinhaExtratoC6 linha = proxima.Value.Value;
            Result<LancamentoContabilizado> lancamento = Converter(linha);
            if (lancamento.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoComSaldo>>($"A linha {NumeroDaLinha(csv)} do arquivo {lancamento.Error}");

            lidos.Add((lancamento.Value, SaldoDiario.Criar(linha.DataContabil, linha.SaldoDoDia)));
        }
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaExtratoC6>>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaExtratoC6>() : Maybe<LinhaExtratoC6>.None;
        }
        catch (CsvHelperException)
        {
            return Result.Failure<Maybe<LinhaExtratoC6>>($"A linha {NumeroDaLinha(csv)} do arquivo não está no layout do extrato da conta do C6.");
        }
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoContabilizado> Converter(LinhaExtratoC6 linha)
    {
        return Result
            .FailureIf(linha.Entrada != 0m && linha.Saida != 0m, "tem entrada e saída no mesmo lançamento.")
            .Ensure(() => !string.IsNullOrWhiteSpace(linha.Titulo), "não tem título.")
            .Map(() => Contabilizar(linha));
    }

    /// <param name="linha">Com título e com só uma das colunas de valor preenchida.</param>
    private static LancamentoContabilizado Contabilizar(LinhaExtratoC6 linha)
    {
        Reais valor = linha.Entrada - linha.Saida;
        (TextoObrigatorio descricao, string? contraparte) = DescricaoEContraparte(linha.Titulo, linha.Descricao);

        // A identidade usa as colunas cruas, para que mudar a descrição montada acima não mude as chaves.
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(linha.DataLancamento)
            .ComValor(valor)
            .ComTexto(linha.Titulo)
            .ComTexto(linha.Descricao);

        Lancamento lancamento = Lancamento.Criar(
            linha.DataLancamento, hora: null, valor, descricao, contraparte, parcela: null, categoriaBanco: null, identidade);

        return LancamentoContabilizado.Criar(lancamento, linha.DataContabil);
    }

    /// <summary>
    /// Num Pix com o nome da contraparte no título, o nome sai da descrição e vira a contraparte. A coluna
    /// Descrição entra depois do título, a não ser que venha vazia ou repita o título, porque aí traria o
    /// nome de volta.
    /// </summary>
    /// <param name="titulo">Não está em branco.</param>
    private static (TextoObrigatorio Descricao, string? Contraparte) DescricaoEContraparte(string titulo, string observacao)
    {
        string tituloAparado = titulo.Trim();
        string observacaoAparada = observacao.Trim();

        string? prefixo = Array.Find(_prefixosPixComContraparte, p => tituloAparado.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        string inicio = prefixo is null ? tituloAparado : tituloAparado[..prefixo.Length];
        string? contraparte = prefixo is null ? null : tituloAparado[prefixo.Length..];

        bool descartarObservacao = observacaoAparada.Length == 0
            || observacaoAparada.Equals(tituloAparado, StringComparison.OrdinalIgnoreCase);

        string descricao = descartarObservacao ? inicio : $"{inicio}{SeparadorDescricao}{observacaoAparada}";
        return ((TextoObrigatorio)descricao, contraparte);
    }

    private static long NumeroDaLinha(CsvReader csv) => LinhasAntesDasColunas + csv.Parser.RawRow;

    [GeneratedRegex(@"Conta:\s*(?<numero>\S+)")]
    private static partial Regex NumeroConta();

    [GeneratedRegex(@"^\d+X?$", RegexOptions.IgnoreCase)]
    private static partial Regex NumeroContaComDigitoColado();

    [GeneratedRegex(@"Extrato de (?<inicio>\d{2}/\d{2}/\d{4}) a (?<fim>\d{2}/\d{2}/\d{4})")]
    private static partial Regex Periodo();

    private sealed class LinhaExtratoC6
    {
        public DateOnly DataLancamento { get; init; }
        public DateOnly DataContabil { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public decimal Entrada { get; init; }
        public decimal Saida { get; init; }
        public decimal SaldoDoDia { get; init; }
    }

    private sealed class LinhaExtratoC6Map : ClassMap<LinhaExtratoC6>
    {
        public const string FormatoData = "dd/MM/yyyy";

        // Entrada e saída vêm sempre positivas; só o saldo pode ser negativo. Nenhuma coluna tem separador de milhar.
        private const NumberStyles ValorSemSinal = NumberStyles.AllowDecimalPoint;
        private const NumberStyles ValorComSinal = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        public LinhaExtratoC6Map()
        {
            Map(l => l.DataLancamento).Name("Data Lançamento").TypeConverterOption.Format(FormatoData);
            Map(l => l.DataContabil).Name("Data Contábil").TypeConverterOption.Format(FormatoData);
            Map(l => l.Titulo).Name("Título");
            Map(l => l.Descricao).Name("Descrição");
            Map(l => l.Entrada).Name("Entrada(R$)").TypeConverterOption.NumberStyles(ValorSemSinal);
            Map(l => l.Saida).Name("Saída(R$)").TypeConverterOption.NumberStyles(ValorSemSinal);
            Map(l => l.SaldoDoDia).Name("Saldo do Dia(R$)").TypeConverterOption.NumberStyles(ValorComSinal);
        }
    }
}
