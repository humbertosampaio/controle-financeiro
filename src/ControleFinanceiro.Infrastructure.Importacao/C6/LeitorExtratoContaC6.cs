using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ContaEPeriodo = (ControleFinanceiro.Domain.Contas.SufixoConta Sufixo, ControleFinanceiro.Domain.Importacoes.PeriodoArquivo Periodo);
using DatasPeriodo = (System.DateOnly Inicio, System.DateOnly Fim);
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
/// As mensagens de falha citam só o número da linha. A <see cref="CsvFormatException"/> pode trazer dados do
/// registro, então ela é capturada e trocada por uma mensagem própria.
/// </para>
/// </remarks>
public sealed partial class LeitorExtratoContaC6 : ILeitorExtrato
{
    private const int LinhasAntesColunas = 8;
    private const string SeparadorDescricao = " · ";
    private const string FormatoData = "dd/MM/yyyy";

    /// <summary>Títulos de Pix que trazem o nome da contraparte depois do prefixo.</summary>
    private static readonly string[] _prefixosPixComContraparte = ["Pix recebido de", "Pix enviado para", "Devol recebida pix de"];

    private static readonly CsvReaderOptions _opcoes = OpcoesLeituraCsv.Criar(separador: ',');

    /// <summary>As colunas mapeadas, com a grafia exata: o CsvHelper aceitaria outra caixa e espaços em volta.</summary>
    private static readonly string[] _colunas =
        ["Data Lançamento", "Data Contábil", "Título", "Descrição", "Entrada(R$)", "Saída(R$)", "Saldo do Dia(R$)"];

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
        for (int i = 0; i < LinhasAntesColunas; i++)
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
            .Bind(LerDatasPeriodo)
            .Bind(datas => PeriodoArquivo.Criar(datas.Inicio, datas.Fim));
    }

    private static Result<DatasPeriodo> LerDatasPeriodo(Match periodo)
    {
        return LerDataPeriodo(periodo.Groups["inicio"].Value)
            .Bind(inicio => LerDataPeriodo(periodo.Groups["fim"].Value).Map(fim => (inicio, fim)));
    }

    private static Result<DateOnly> LerDataPeriodo(string texto)
    {
        return Result.SuccessIf(
            DateOnly.TryParseExact(texto, FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly data),
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
                return Result.Failure("O arquivo termina antes das colunas do extrato da conta do C6.");

            return Result.FailureIf(
                !_colunas.All(csv.Headers.Contains),
                $"A linha {LinhasAntesColunas + 1} do arquivo não traz as colunas do extrato da conta do C6.");
        }
        catch (CsvFormatException)
        {
            return Result.Failure($"A linha {LinhasAntesColunas + 1} do arquivo não traz as colunas do extrato da conta do C6.");
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
                return Result.Failure<IReadOnlyList<LancamentoComSaldo>>($"A linha {NumeroLinha(csv)} do arquivo {lancamento.Error}");

            lidos.Add((lancamento.Value, SaldoDiario.Criar(linha.DataContabil, linha.SaldoDia)));
        }
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaExtratoC6>>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaExtratoC6>() : Maybe<LinhaExtratoC6>.None;
        }
        catch (CsvFormatException erro)
        {
            return Result.Failure<Maybe<LinhaExtratoC6>>($"A linha {LinhasAntesColunas + erro.LineNumber} do arquivo não está no layout do extrato da conta do C6.");
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

        Lancamento lancamento = Lancamento.DaConta(
            linha.DataLancamento, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

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

    private static long NumeroLinha(CsvReader csv) => LinhasAntesColunas + csv.LineNumber;

    [GeneratedRegex(@"Conta:\s*(?<numero>\S+)")]
    private static partial Regex NumeroConta();

    [GeneratedRegex(@"^\d+X?$", RegexOptions.IgnoreCase)]
    private static partial Regex NumeroContaComDigitoColado();

    [GeneratedRegex(@"Extrato de (?<inicio>\d{2}/\d{2}/\d{4}) a (?<fim>\d{2}/\d{2}/\d{4})")]
    private static partial Regex Periodo();

    [CsvRecord]
    internal sealed partial class LinhaExtratoC6
    {
        [CsvColumn("Data Lançamento"), CsvFormat(FormatoData)]
        public DateOnly DataLancamento { get; init; }

        [CsvColumn("Data Contábil"), CsvFormat(FormatoData)]
        public DateOnly DataContabil { get; init; }

        [CsvColumn("Título")]
        public string Titulo { get; init; } = string.Empty;

        [CsvColumn("Descrição")]
        public string Descricao { get; init; } = string.Empty;

        // Entrada e saída vêm sempre positivas; só o saldo pode ser negativo.
        [CsvColumn("Entrada(R$)"), CsvConverter(typeof(ValorC6.SemSinalConverter))]
        public decimal Entrada { get; init; }

        [CsvColumn("Saída(R$)"), CsvConverter(typeof(ValorC6.SemSinalConverter))]
        public decimal Saida { get; init; }

        [CsvColumn("Saldo do Dia(R$)"), CsvConverter(typeof(ValorC6.ComSinalConverter))]
        public decimal SaldoDia { get; init; }
    }
}
