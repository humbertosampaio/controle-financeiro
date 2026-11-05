using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura.Respostas;

/// <summary>A resposta do <c>listar_transacoes</c> num mês válido.</summary>
internal sealed class TransacoesDoMesLidas
{
    public string Mes { get; }
    public IReadOnlyList<TransacaoLida> Transacoes { get; }

    public TransacoesDoMesLidas(JsonObject json)
    {
        Mes = json.Texto("mes");
        Transacoes = json["transacoes"]!
            .AsArray()
            .Select(transacao => new TransacaoLida(transacao!.AsObject()))
            .ToArray();
    }
}
