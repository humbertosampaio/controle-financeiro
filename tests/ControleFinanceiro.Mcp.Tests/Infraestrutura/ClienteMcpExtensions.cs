using ControleFinanceiro.Application.IA;
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
        /// <param name="sufixoConta">Só no extrato sem a conta no arquivo; nulo, o argumento não vai.</param>
        public ValueTask<CallToolResult> PreverImportacaoAsync(string caminho, string layout, string? sufixoConta = null, string? vencimento = null)
        {
            return clienteMcp.CallToolAsync(Tools.PreverImportacao, ArgumentosDaImportacao(caminho, layout, sufixoConta, vencimento));
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada, com o resumo primeiro.</summary>
        public ValueTask<CallToolResult> ConfirmarImportacaoAsync(
            string resumo,
            string caminho,
            string layout,
            string? sufixoConta = null,
            string? vencimento = null)
        {
            Dictionary<string, object?> argumentos = new() { ["resumo"] = resumo };
            foreach ((string nome, object? valor) in ArgumentosDaImportacao(caminho, layout, sufixoConta, vencimento))
                argumentos[nome] = valor;

            return clienteMcp.CallToolAsync(Tools.ConfirmarImportacao, argumentos);
        }
    }

    private static Dictionary<string, object?> ArgumentosDaImportacao(string caminho, string layout, string? sufixoConta, string? vencimento)
    {
        Dictionary<string, object?> argumentos = new() { ["caminho"] = caminho, ["layout"] = layout };
        if (sufixoConta is not null)
            argumentos["sufixoConta"] = sufixoConta;

        if (vencimento is not null)
            argumentos["vencimento"] = vencimento;

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
