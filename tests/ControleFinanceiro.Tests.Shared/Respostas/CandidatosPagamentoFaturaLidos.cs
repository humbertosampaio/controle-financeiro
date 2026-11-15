using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

/// <summary>A resposta do <c>listar_candidatos_pagamento_fatura</c>.</summary>
public sealed class CandidatosPagamentoFaturaLidos
{
    public string Mes { get; }
    public IReadOnlyList<CandidatoPagamentoFaturaLido> Candidatos { get; }

    public CandidatosPagamentoFaturaLidos(JsonObject json)
    {
        Mes = json.Texto("mes");
        Candidatos = json["candidatos"]!
            .AsArray()
            .Select(candidato => new CandidatoPagamentoFaturaLido(candidato!.AsObject()))
            .ToArray();
    }
}
