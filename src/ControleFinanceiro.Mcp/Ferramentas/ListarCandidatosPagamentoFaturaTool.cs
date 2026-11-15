using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Leituras;
using ControleFinanceiro.Application.IA.Saida;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class ListarCandidatosPagamentoFaturaTool
{
    [McpServerTool(Name = Tools.ListarCandidatosPagamentoFatura, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"Lista as saídas da conta que provavelmente pagaram as faturas de cartão que vencem num mês e que ainda não foram ligadas a elas: a saída tem exatamente o valor do total da fatura e a data perto do vencimento, e só vale o par sem empate. Devolve, por candidato, a saída, o id de um cartão da fatura, o banco, o mês e a data de vencimento e o total. Nada é gravado: para ligar, chame {Tools.PreverPagamentoFatura} com o id da saída, o idCartao e o ano e o mês do vencimento do candidato.")]
    public static Task<CandidatosPagamentoFaturaParaModelo> Listar(
        LeituraParaModeloService leitura,
        [Description("O ano em que as faturas vencem, com quatro dígitos.")] int ano,
        [Description("O mês em que as faturas vencem, de 1 a 12.")] int mes,
        CancellationToken cancellationToken)
    {
        return leitura.ListarCandidatosPagamentoFaturaAsync(ano, mes, cancellationToken);
    }
}
