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

    public FormatoExtrato Formato => FormatoExtrato.C6ContaCsv;

    public async Task<Result<Extrato>> LerAsync(Stream arquivo, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        List<string> linhasAntesDasColunas = [];
        for (int i = 0; i < LinhasAntesDasColunas; i++)
        {
            string? linha = await leitor.ReadLineAsync(cancellationToken);
            if (linha is null)
                return Result.Failure<Extrato>("O arquivo termina antes das colunas do extrato da conta do C6.");

            linhasAntesDasColunas.Add(linha);
        }

        Result<SufixoConta> sufixo = LerSufixoConta(linhasAntesDasColunas);
        if (sufixo.IsFailure)
            return Result.Failure<Extrato>(sufixo.Error);

        Result<PeriodoExtrato> periodo = LerPeriodo(linhasAntesDasColunas);
        if (periodo.IsFailure)
            return Result.Failure<Extrato>(periodo.Error);

        using CsvReader csv = new(leitor, _configuracao);
        csv.Context.RegisterClassMap<LinhaExtratoC6Map>();

        try
        {
            if (!await csv.ReadAsync())
                return Result.Failure<Extrato>("O arquivo termina antes das colunas do extrato da conta do C6.");

            csv.ReadHeader();
            csv.ValidateHeader<LinhaExtratoC6>();
        }
        catch (CsvHelperException)
        {
            return Result.Failure<Extrato>(
                $"A linha {LinhasAntesDasColunas + 1} do arquivo não traz as colunas do extrato da conta do C6.");
        }

        List<LancamentoContabilizado> lancamentos = [];
        List<SaldoDiario> saldos = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LinhaExtratoC6 linha;
            try
            {
                if (!await csv.ReadAsync())
                    break;

                linha = csv.GetRecord<LinhaExtratoC6>();
            }
            catch (CsvHelperException)
            {
                return Result.Failure<Extrato>($"A linha {NumeroDaLinha(csv)} do arquivo não está no layout do extrato da conta do C6.");
            }

            Result<LancamentoContabilizado> lancamento = Converter(linha);
            if (lancamento.IsFailure)
                return Result.Failure<Extrato>($"A linha {NumeroDaLinha(csv)} do arquivo {lancamento.Error}");

            lancamentos.Add(lancamento.Value);
            saldos.Add(SaldoDiario.Criar(linha.DataContabil, linha.SaldoDoDia));
        }

        // Um saldo por dia contábil: o C6 repete o saldo em toda linha do dia. Se as linhas de um dia
        // trouxerem saldos diferentes, os dois ficam, e o extrato não nasce, com a falha apontando o dia.
        return Extrato.Criar(Banco.C6, TipoConta.ContaCorrente, sufixo.Value, periodo.Value, lancamentos, saldos.Distinct());
    }

    private static Result<SufixoConta> LerSufixoConta(IEnumerable<string> linhas)
    {
        string? numeroConta = linhas
            .Select(l => NumeroConta().Match(l))
            .Where(m => m.Success)
            .Select(m => m.Groups["numero"].Value)
            .FirstOrDefault();

        if (numeroConta is null)
            return Result.Failure<SufixoConta>("O cabeçalho do arquivo não traz a conta do extrato.");

        // O C6 escreve o número com o dígito verificador colado. Em outro formato (o dígito separado por
        // hífen, por exemplo), os 4 últimos caracteres não seriam o sufixo: melhor falhar que associar outra conta.
        if (!NumeroContaComDigitoColado().IsMatch(numeroConta))
            return Result.Failure<SufixoConta>("O número da conta no cabeçalho do arquivo não está no formato do C6.");

        return numeroConta.Length < SufixoConta.QuantidadeCaracteres
            ? Result.Failure<SufixoConta>("O número da conta no cabeçalho do arquivo é curto demais.")
            : SufixoConta.Criar(numeroConta[^SufixoConta.QuantidadeCaracteres..]);
    }

    private static Result<PeriodoExtrato> LerPeriodo(IEnumerable<string> linhas)
    {
        Match? periodo = linhas
            .Select(l => Periodo().Match(l))
            .FirstOrDefault(m => m.Success);

        if (periodo is null)
            return Result.Failure<PeriodoExtrato>("O cabeçalho do arquivo não traz o período do extrato.");

        if (!LerData(periodo.Groups["inicio"].Value, out DateOnly inicio) || !LerData(periodo.Groups["fim"].Value, out DateOnly fim))
            return Result.Failure<PeriodoExtrato>("O período no cabeçalho do arquivo não tem datas válidas.");

        return PeriodoExtrato.Criar(inicio, fim);
    }

    private static bool LerData(string texto, out DateOnly data)
    {
        return DateOnly.TryParseExact(texto, LinhaExtratoC6Map.FormatoData, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoContabilizado> Converter(LinhaExtratoC6 linha)
    {
        if (linha.Entrada != 0m && linha.Saida != 0m)
            return Result.Failure<LancamentoContabilizado>("tem entrada e saída no mesmo lançamento.");

        if (string.IsNullOrWhiteSpace(linha.Titulo))
            return Result.Failure<LancamentoContabilizado>("não tem título.");

        Reais valor = linha.Entrada - linha.Saida;
        (TextoObrigatorio descricao, string? contraparte) = DescricaoEContraparte(linha.Titulo, linha.Descricao);

        // A identidade usa as colunas cruas, para que mudar a descrição montada acima não mude as chaves.
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(linha.DataLancamento)
            .ComValor(valor)
            .ComTexto(linha.Titulo)
            .ComTexto(linha.Descricao);

        Lancamento lancamento = Lancamento.Criar(
            linha.DataLancamento, hora: null, valor, descricao, contraparte, identidade);

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
