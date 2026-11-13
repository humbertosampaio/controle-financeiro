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
                "prever_vinculo",
                new Dictionary<string, object?> { ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada.</summary>
        public ValueTask<CallToolResult> ConfirmarVinculoAsync(Guid idTransacao, Guid idDespesa, string resumo)
        {
            return clienteMcp.CallToolAsync(
                "confirmar_vinculo",
                new Dictionary<string, object?> { ["resumo"] = resumo, ["idTransacao"] = idTransacao, ["idDespesa"] = idDespesa });
        }
    }

    extension(McpClient clienteMcp)
    {
        public ValueTask<CallToolResult> ListarCartoesAsync() => clienteMcp.CallToolAsync("listar_cartoes", new Dictionary<string, object?>());

        public ValueTask<CallToolResult> PreverPagamentoFaturaAsync(Guid idTransacao, Guid idCartao, MesReferencia mesVencimento)
        {
            return clienteMcp.CallToolAsync("prever_pagamento_fatura", ArgumentosDoPagamento(idTransacao, idCartao, mesVencimento));
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada, com o resumo primeiro.</summary>
        public ValueTask<CallToolResult> ConfirmarPagamentoFaturaAsync(Guid idTransacao, Guid idCartao, MesReferencia mesVencimento, string resumo)
        {
            Dictionary<string, object?> argumentos = new() { ["resumo"] = resumo };
            foreach ((string nome, object? valor) in ArgumentosDoPagamento(idTransacao, idCartao, mesVencimento))
                argumentos[nome] = valor;

            return clienteMcp.CallToolAsync("confirmar_pagamento_fatura", argumentos);
        }
    }

    extension(McpClient clienteMcp)
    {
        /// <param name="sufixoConta">Só no extrato sem a conta no arquivo; nulo, o argumento não vai.</param>
        public ValueTask<CallToolResult> PreverImportacaoAsync(string caminho, string layout, string? sufixoConta = null)
        {
            return clienteMcp.CallToolAsync("prever_importacao", ArgumentosDaImportacao(caminho, layout, sufixoConta));
        }

        /// <summary>Chama a confirmação como o cliente a envia depois que o usuário aprova a chamada, com o resumo primeiro.</summary>
        public ValueTask<CallToolResult> ConfirmarImportacaoAsync(string resumo, string caminho, string layout, string? sufixoConta = null)
        {
            Dictionary<string, object?> argumentos = new() { ["resumo"] = resumo };
            foreach ((string nome, object? valor) in ArgumentosDaImportacao(caminho, layout, sufixoConta))
                argumentos[nome] = valor;

            return clienteMcp.CallToolAsync("confirmar_importacao", argumentos);
        }
    }

    private static Dictionary<string, object?> ArgumentosDaImportacao(string caminho, string layout, string? sufixoConta)
    {
        Dictionary<string, object?> argumentos = new() { ["caminho"] = caminho, ["layout"] = layout };
        if (sufixoConta is not null)
            argumentos["sufixoConta"] = sufixoConta;

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
