using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Chama as ferramentas como o modelo chamaria: as de leitura só com o ano e o mês, as do vínculo com os ids, e as do
/// pagamento da fatura com os ids e o mês de vencimento.
/// </summary>
internal static class ClienteMcpExtensions
{
    extension(McpClient clienteMcp)
    {
        public ValueTask<CallToolResult> ChamarDoMesAsync(string ferramenta, int ano, int mes)
        {
            return clienteMcp.CallToolAsync(ferramenta, new Dictionary<string, object?> { ["ano"] = ano, ["mes"] = mes });
        }

        public async Task<McpClientTool> ObterFerramentaAsync(string nome)
        {
            IList<McpClientTool> ferramentas = await clienteMcp.ListToolsAsync();
            return ferramentas.Single(f => f.Name == nome);
        }

        public ValueTask<CallToolResult> PreverVinculoAsync(Guid idTransacao, Guid idDespesa)
        {
            return clienteMcp.CallToolAsync(
                Tools.PreverVinculo,
                new Dictionary<string, object?> { ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada.</summary>
        public ValueTask<CallToolResult> ConfirmarVinculoAsync(Guid idTransacao, Guid idDespesa, string resumo)
        {
            return clienteMcp.CallToolAsync(
                Tools.ConfirmarVinculo,
                new Dictionary<string, object?> { ["resumo"] = resumo, ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }
    }

    extension(McpClient clienteMcp)
    {
        public ValueTask<CallToolResult> ListarCartoesAsync() => clienteMcp.CallToolAsync(Tools.ListarCartoes, new Dictionary<string, object?>());

        public ValueTask<CallToolResult> ListarCandidatosPagamentoFaturaAsync(MesReferencia mesVencimento) =>
            clienteMcp.ChamarDoMesAsync(Tools.ListarCandidatosPagamentoFatura, mesVencimento.Ano, mesVencimento.Mes);

        public ValueTask<CallToolResult> PreverPagamentoFaturaAsync(Guid idTransacao, Guid idCartao, MesReferencia mesVencimento)
        {
            return clienteMcp.CallToolAsync(Tools.PreverPagamentoFatura, ArgumentosDoPagamento(idTransacao, idCartao, mesVencimento));
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada, com o resumo primeiro.</summary>
        public ValueTask<CallToolResult> ConfirmarPagamentoFaturaAsync(Guid idTransacao, Guid idCartao, MesReferencia mesVencimento, string resumo)
        {
            Dictionary<string, object?> argumentos = new() { ["resumo"] = resumo };
            foreach ((string nome, object? valor) in ArgumentosDoPagamento(idTransacao, idCartao, mesVencimento))
                argumentos[nome] = valor;

            return clienteMcp.CallToolAsync(Tools.ConfirmarPagamentoFatura, argumentos);
        }
    }

    extension(McpClient clienteMcp)
    {
        /// <summary>
        /// Chama a prévia do layout só com os argumentos dele. O sufixo e o vencimento vão só no layout que os pede, com o
        /// valor das fixtures quando nulos.
        /// </summary>
        public ValueTask<CallToolResult> PreverImportacaoAsync(
            LayoutArquivoImportacao layout,
            string caminho,
            string? sufixoConta = null,
            string? vencimento = null)
        {
            return clienteMcp.CallToolAsync(_previaPorLayout[layout], ArgumentosImportacao(layout, caminho, sufixoConta, vencimento));
        }

        /// <summary>
        /// Chama a confirmação do layout como o cliente a envia depois que o usuário aprova a chamada, com o resumo
        /// primeiro, pela regra dos argumentos do <see cref="PreverImportacaoAsync(McpClient, LayoutArquivoImportacao, string, string?, string?)"/>.
        /// </summary>
        public ValueTask<CallToolResult> ConfirmarImportacaoAsync(
            LayoutArquivoImportacao layout,
            string resumo,
            string caminho,
            string? sufixoConta = null,
            string? vencimento = null)
        {
            Dictionary<string, object?> argumentos = new() { ["resumo"] = resumo };
            foreach ((string nome, object? valor) in ArgumentosImportacao(layout, caminho, sufixoConta, vencimento))
                argumentos[nome] = valor;

            return clienteMcp.CallToolAsync(_confirmacaoPorLayout[layout], argumentos);
        }
    }

    private static readonly Dictionary<LayoutArquivoImportacao, string> _previaPorLayout = new()
    {
        [LayoutArquivoImportacao.C6ContaCsv] = Tools.PreverImportacaoC6ContaCsv,
        [LayoutArquivoImportacao.MercadoPagoContaCsv] = Tools.PreverImportacaoMercadoPagoContaCsv,
        [LayoutArquivoImportacao.C6CartaoCsv] = Tools.PreverImportacaoC6CartaoCsv,
        [LayoutArquivoImportacao.MercadoPagoCartaoCsv] = Tools.PreverImportacaoMercadoPagoCartaoCsv,
    };

    private static readonly Dictionary<LayoutArquivoImportacao, string> _confirmacaoPorLayout = new()
    {
        [LayoutArquivoImportacao.C6ContaCsv] = Tools.ConfirmarImportacaoC6ContaCsv,
        [LayoutArquivoImportacao.MercadoPagoContaCsv] = Tools.ConfirmarImportacaoMercadoPagoContaCsv,
        [LayoutArquivoImportacao.C6CartaoCsv] = Tools.ConfirmarImportacaoC6CartaoCsv,
        [LayoutArquivoImportacao.MercadoPagoCartaoCsv] = Tools.ConfirmarImportacaoMercadoPagoCartaoCsv,
    };

    private static Dictionary<string, object?> ArgumentosImportacao(
        LayoutArquivoImportacao layout,
        string caminho,
        string? sufixoConta,
        string? vencimento)
    {
        Dictionary<string, object?> argumentos = new() { ["caminho"] = caminho };
        if (LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout))
            argumentos["sufixoConta"] = sufixoConta ?? ImportacaoDeTeste.SufixoContaMercadoPago;

        if (LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout))
            argumentos["vencimento"] = vencimento ?? ImportacaoDeTeste.VencimentoFaturaC6;

        return argumentos;
    }

    private static Dictionary<string, object?> ArgumentosDoPagamento(Guid idTransacao, Guid idCartao, MesReferencia mesVencimento)
    {
        return new Dictionary<string, object?>
        {
            ["idTransacao"] = idTransacao,
            ["idCartao"] = idCartao,
            ["ano"] = mesVencimento.Ano,
            ["mes"] = mesVencimento.Mes,
        };
    }

    extension(CallToolResult resultado)
    {
        /// <summary>O texto que o modelo recebe, exatamente como o servidor o escreveu.</summary>
        public string Texto => resultado.Content.OfType<TextContentBlock>().Single().Text;

        public JsonObject Json => JsonNode.Parse(resultado.Texto)!.AsObject();
    }
}
