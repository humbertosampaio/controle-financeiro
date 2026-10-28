using ControleFinanceiro.Domain.Despesas;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarRecorrentes;

/// <summary>As despesas recorrentes do usuário, para a tela do cadastro.</summary>
public sealed class ListarDespesasRecorrentesEndpoint : Endpoint<ListarDespesasRecorrentesRequest, ListarDespesasRecorrentesResponse>
{
    private readonly IDespesaRecorrenteRepository _despesaRecorrenteRepository;

    public ListarDespesasRecorrentesEndpoint(IDespesaRecorrenteRepository despesaRecorrenteRepository)
    {
        _despesaRecorrenteRepository = despesaRecorrenteRepository;
    }

    public override void Configure()
    {
        Get("usuarios/{idUsuario}/despesas-recorrentes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListarDespesasRecorrentesRequest req, CancellationToken ct)
    {
        IReadOnlyList<DespesaRecorrente> despesasRecorrentes = await _despesaRecorrenteRepository.ListarAsync(ct);

        ListarDespesasRecorrentesResponse.DespesaRecorrente[] despesasResponse = despesasRecorrentes
            .OrderBy(d => d.DiaOcorrencia)
            .ThenBy(d => d.Descricao.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Ler)
            .ToArray();

        await Send.OkAsync(
            new ListarDespesasRecorrentesResponse(despesasResponse),
            ct);
    }

    private static ListarDespesasRecorrentesResponse.DespesaRecorrente Ler(DespesaRecorrente despesaRecorrente)
    {
        return new(
            despesaRecorrente.Id,
            despesaRecorrente.IdBeneficiario,
            despesaRecorrente.Descricao,
            despesaRecorrente.IdCategoria,
            despesaRecorrente.FormaPagamento.Value,
            despesaRecorrente.IdCartao,
            despesaRecorrente.Valor,
            despesaRecorrente.DiaOcorrencia);
    }
}
