using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>O que o usuário precisa resolver pela API, numa importação que falhou.</summary>
public sealed class PendenciasImportacaoLidas
{
    public int Beneficiarios { get; }
    public int Categorias { get; }
    public IReadOnlyList<string> FinaisSemCartao { get; }

    public PendenciasImportacaoLidas(JsonObject json)
    {
        Beneficiarios = json["beneficiarios"]!.GetValue<int>();
        Categorias = json["categorias"]!.GetValue<int>();
        FinaisSemCartao = json["finais_sem_cartao"]!.AsArray().Select(final => final!.GetValue<string>()).ToArray();
    }

    /// <summary>As pendências de uma resposta que falhou, no campo <c>pendencias</c>.</summary>
    public static PendenciasImportacaoLidas DoEnvelope(JsonObject json) => new(json["pendencias"]!.AsObject());
}
