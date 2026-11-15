using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// Variações da fatura fictícia do C6 (<c>Fixtures/C6/fatura-cartao.csv</c>, do projeto de testes da importação), no
/// mesmo layout: UTF-8 sem BOM e CRLF. O arquivo tem 8 lançamentos, 4 de cada final (1111, depois 2222), com a
/// parcela 9/10 de uma compra de 15/07/2025 no final 1111, o pagamento da fatura anterior no final 1111 e duas compras
/// idênticas no final 2222.
/// </summary>
internal static class FaturaCartaoC6
{
    private const string ParcelaCompraAntiga = "LOJA DE EXEMPLO;9/10";
    private const string RotuloPagamento = "Inclusao de Pagamento";
    private const int ColunaFinal = 2;

    private static readonly string _caminho = Path.Combine(AppContext.BaseDirectory, "Importacoes", "ImportarFatura", "Fixtures", "fatura-cartao.csv");

    public static readonly SufixoCartao SufixoFisico = (SufixoCartao)"1111";
    public static readonly SufixoCartao SufixoVirtual = (SufixoCartao)"2222";
    public const int LancamentosPorCartao = 4;
    public const int QuantidadeLancamentos = 8;
    public const string TitularFicticio = "FULANA DE TAL FICTICIA";
    public static readonly DateOnly PrimeiraCompraFisico = new(2025, 7, 15);
    public static readonly DateOnly UltimaCompraFisico = new(2026, 3, 10);

    /// <summary>
    /// O vencimento que o usuário informa, que o arquivo não traz: o da fatura com as compras de março, com o vencimento no
    /// dia 10 e o fechamento no dia 3 que os testes dão aos cartões.
    /// </summary>
    public static readonly DateOnly Vencimento = new(2026, 4, 10);

    /// <summary>O mês do <see cref="Vencimento"/>: o de todas as compras da fatura, inclusive a de fevereiro.</summary>
    public static readonly MesReferencia MesVencimento = (MesReferencia)(04, 2026);

    /// <summary>O arquivo como está em disco.</summary>
    public static byte[] Completo() => File.ReadAllBytes(_caminho);

    /// <summary>A fatura seguinte da compra parcelada: a mesma compra antiga, agora na parcela 10/10.</summary>
    public static byte[] ComParcelaSeguinte()
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8).Replace(ParcelaCompraAntiga, "LOJA DE EXEMPLO;10/10", StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(conteudo);
    }

    /// <summary>A fatura em que a linha do pagamento da fatura anterior vira outra entrada, como num rótulo novo do banco.</summary>
    public static byte[] SemPagamento()
    {
        string conteudo = File.ReadAllText(_caminho, Encoding.UTF8).Replace(RotuloPagamento, "CREDITO FICTICIO", StringComparison.Ordinal);
        return Encoding.UTF8.GetBytes(conteudo);
    }

    /// <summary>
    /// A fatura só com as linhas de um final, como um arquivo parcial: o cabeçalho e os <see cref="LancamentosPorCartao"/>
    /// lançamentos do <paramref name="sufixo"/>, que o arquivo traz ora sem aspas, ora com.
    /// </summary>
    public static byte[] ApenasLinhasFinal(SufixoCartao sufixo)
    {
        const string QuebraLinha = "\r\n";
        string[] linhas = File.ReadAllText(_caminho, Encoding.UTF8).Split(QuebraLinha, StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> linhasFinal = linhas
            .Skip(1)
            .Where(linha => linha.Split(';')[ColunaFinal].Trim('"') == sufixo.Value);

        return Encoding.UTF8.GetBytes(string.Join(QuebraLinha, linhasFinal.Prepend(linhas[0])) + QuebraLinha);
    }

    /// <summary>A fatura com duas compras idênticas parceladas: a linha da parcela 9/10 aparece duas vezes.</summary>
    public static byte[] ComCompraParceladaGemea()
    {
        const string QuebraDeLinha = "\r\n";
        IEnumerable<string> linhas = File.ReadAllText(_caminho, Encoding.UTF8)
            .Split(QuebraDeLinha)
            .SelectMany(linha => linha.Contains(ParcelaCompraAntiga, StringComparison.Ordinal) ? [linha, linha] : new[] { linha });

        return Encoding.UTF8.GetBytes(string.Join(QuebraDeLinha, linhas));
    }
}
