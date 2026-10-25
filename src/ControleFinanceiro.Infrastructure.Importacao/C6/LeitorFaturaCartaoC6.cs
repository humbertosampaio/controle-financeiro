using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SufixoEParcela = (ControleFinanceiro.Domain.Cartoes.SufixoCartao Sufixo, ControleFinanceiro.Domain.Transacoes.Parcela? Parcela);

namespace ControleFinanceiro.Infrastructure.Importacao.C6;

/// <summary>
/// Lê a fatura CSV do cartão do C6: as colunas na primeira linha, separadas por <c>;</c>, e depois uma compra por
/// linha, com o final do cartão que a fez.
/// </summary>
/// <remarks>
/// <para>
/// O nome do titular, o valor em dólar e a cotação não são mapeados: o titular não sai do arquivo, e o valor em
/// reais já é o cobrado.
/// </para>
/// <para>
/// As mensagens de falha citam só o número da linha. A <see cref="CsvHelperException"/> traz o registro cru, com o
/// titular, então ela é capturada e trocada por uma mensagem própria.
/// </para>
/// </remarks>
public sealed partial class LeitorFaturaCartaoC6 : ILeitorFatura
{
    private const string ParcelaUnica = "Única";

    private static readonly CsvConfiguration _configuracao = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ";",
        DetectColumnCountChanges = true,
    };

    public LayoutArquivoImportacao Layout => LayoutArquivoImportacao.C6CartaoCsv;

    /// <summary>Lê as colunas, depois cada compra, e monta a fatura.</summary>
    public async Task<Result<Fatura>> LerAsync(Stream arquivo, CancellationToken cancellationToken)
    {
        using StreamReader leitor = new(arquivo, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using CsvReader csv = new(leitor, _configuracao);
        csv.Context.RegisterClassMap<LinhaFaturaC6Map>();

        return await LerColunasAsync(csv)
            .Bind(() => LerLinhasAsync(csv, cancellationToken))
            .Bind(lancamentos => Fatura.Criar(Banco.C6, lancamentos));
    }

    private static async Task<Result> LerColunasAsync(CsvReader csv)
    {
        try
        {
            if (!await csv.ReadAsync())
                return Result.Failure("O arquivo termina antes das colunas da fatura do cartão do C6.");

            csv.ReadHeader();
            csv.ValidateHeader<LinhaFaturaC6>();
            return Result.Success();
        }
        catch (CsvHelperException)
        {
            return Result.Failure("A linha 1 do arquivo não traz as colunas da fatura do cartão do C6.");
        }
    }

    /// <summary>Para na primeira linha que não está no layout.</summary>
    private static async Task<Result<IReadOnlyList<LancamentoFatura>>> LerLinhasAsync(
        CsvReader csv,
        CancellationToken cancellationToken)
    {
        List<LancamentoFatura> lidos = [];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Result<Maybe<LancamentoFatura>> proximo = await LerProximaLinhaAsync(csv)
                .Bind(linha => ConverterSeHouver(csv, linha));

            if (proximo.IsFailure)
                return Result.Failure<IReadOnlyList<LancamentoFatura>>(proximo.Error);

            if (proximo.Value.HasNoValue)
                return lidos;

            lidos.Add(proximo.Value.Value);
        }
    }

    /// <param name="linha">Vazia no fim do arquivo.</param>
    private static Result<Maybe<LancamentoFatura>> ConverterSeHouver(CsvReader csv, Maybe<LinhaFaturaC6> linha)
    {
        if (linha.HasNoValue)
            return Maybe<LancamentoFatura>.None;

        return Converter(linha.Value)
            .Map(Maybe.From)
            .MapError(erro => $"A linha {csv.Parser.RawRow} do arquivo {erro}");
    }

    /// <returns>Vazio no fim do arquivo.</returns>
    private static async Task<Result<Maybe<LinhaFaturaC6>>> LerProximaLinhaAsync(CsvReader csv)
    {
        try
        {
            return await csv.ReadAsync() ? csv.GetRecord<LinhaFaturaC6>() : Maybe<LinhaFaturaC6>.None;
        }
        catch (CsvHelperException)
        {
            return Result.Failure<Maybe<LinhaFaturaC6>>($"A linha {csv.Parser.RawRow} do arquivo não está no layout da fatura do cartão do C6.");
        }
    }

    /// <param name="linha">A linha já lida, com as colunas cruas.</param>
    /// <returns>Na falha, o fim da frase que começa com "A linha N do arquivo".</returns>
    private static Result<LancamentoFatura> Converter(LinhaFaturaC6 linha)
    {
        return Result
            .FailureIf(string.IsNullOrWhiteSpace(linha.Descricao), "não tem descrição.")
            .Bind(() => LerSufixoEParcela(linha))
            .Map(sufixoEParcela => Lancar(linha, sufixoEParcela));
    }

    private static Result<SufixoEParcela> LerSufixoEParcela(LinhaFaturaC6 linha)
    {
        return LerSufixo(linha.FinalCartao)
            .Bind(sufixo => LerParcela(linha.Parcela).Map(parcela => (sufixo, parcela)));
    }

    private static Result<SufixoCartao> LerSufixo(string finalCartao)
    {
        return SufixoCartao
            .Criar(finalCartao)
            .MapError(_ => "não tem o final do cartão com 4 dígitos.");
    }

    /// <summary>
    /// <c>Única</c> é a compra à vista, sem parcela, e o <c>1/1</c> também: a compra numa parcela só não é parcelada.
    /// </summary>
    private static Result<Parcela?> LerParcela(string texto)
    {
        string parcela = texto.Trim();
        if (parcela == ParcelaUnica)
            return Result.Success<Parcela?>(null);

        Match numerada = ParcelaNumerada().Match(parcela);
        if (!numerada.Success)
            return Result.Failure<Parcela?>("não tem a parcela no formato do C6.");

        int numero = int.Parse(numerada.Groups["numero"].Value, CultureInfo.InvariantCulture);
        int total = int.Parse(numerada.Groups["total"].Value, CultureInfo.InvariantCulture);
        if (numero == 1 && total == 1)
            return Result.Success<Parcela?>(null);

        return Parcela
            .Criar(numero, total)
            .Map(p => (Parcela?)p)
            .MapError(_ => "não tem a parcela no formato do C6.");
    }

    /// <param name="linha">Com descrição.</param>
    private static LancamentoFatura Lancar(LinhaFaturaC6 linha, SufixoEParcela sufixoEParcela)
    {
        // A fatura traz a compra positiva e o pagamento ou estorno negativo; a transação é do ponto de vista do
        // usuário.
        Reais valor = -linha.Valor;

        // A identidade usa a descrição crua, para que mudar o tratamento da descrição não mude as chaves. O final não
        // entra: o id do cartão já está na chave. A parcela entra, para a 3/10 e a 4/10 de faturas diferentes não
        // colidirem. Limitação aceita: duas compras à vista idênticas no dia do fechamento, uma em cada fatura, têm a
        // mesma chave, e a da segunda fatura é descartada.
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(linha.DataCompra)
            .ComValor(valor)
            .ComTexto(linha.Descricao)
            .ComParcela(sufixoEParcela.Parcela);

        Lancamento lancamento = Lancamento.Criar(
            linha.DataCompra,
            hora: null,
            valor,
            (TextoObrigatorio)linha.Descricao,
            contraparte: null,
            sufixoEParcela.Parcela,
            linha.Categoria,
            identidade);

        return LancamentoFatura.Criar(lancamento, sufixoEParcela.Sufixo);
    }

    [GeneratedRegex(@"^(?<numero>\d{1,3})/(?<total>\d{1,3})$")]
    private static partial Regex ParcelaNumerada();

    private sealed class LinhaFaturaC6
    {
        public DateOnly DataCompra { get; init; }
        public string FinalCartao { get; init; } = string.Empty;
        public string Categoria { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public string Parcela { get; init; } = string.Empty;
        public decimal Valor { get; init; }
    }

    private sealed class LinhaFaturaC6Map : ClassMap<LinhaFaturaC6>
    {
        private const string FormatoData = "dd/MM/yyyy";

        // O valor em reais vem com ponto decimal e sem separador de milhar; o pagamento e o estorno vêm negativos.
        private const NumberStyles ValorComSinal = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        public LinhaFaturaC6Map()
        {
            Map(l => l.DataCompra).Name("Data de Compra").TypeConverterOption.Format(FormatoData);
            Map(l => l.FinalCartao).Name("Final do Cartão");
            Map(l => l.Categoria).Name("Categoria");
            Map(l => l.Descricao).Name("Descrição");
            Map(l => l.Parcela).Name("Parcela");
            Map(l => l.Valor).Name("Valor (em R$)").TypeConverterOption.NumberStyles(ValorComSinal);
        }
    }
}
