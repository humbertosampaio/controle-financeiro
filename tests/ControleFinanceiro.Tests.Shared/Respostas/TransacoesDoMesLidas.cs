using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>A resposta do <c>listar_transacoes</c> num mês válido.</summary>
public sealed class TransacoesDoMesLidas
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
