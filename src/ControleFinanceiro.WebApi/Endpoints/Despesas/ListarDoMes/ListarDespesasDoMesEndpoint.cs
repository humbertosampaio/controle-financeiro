using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

/// <summary>As despesas do usuário num mês de referência, geradas ou não por uma recorrente.</summary>
public sealed class ListarDespesasDoMesEndpoint : Endpoint<ListarDespesasDoMesRequest, ListarDespesasDoMesResponse>
{
    private readonly IDespesaRepository _despesaRepository;

    public ListarDespesasDoMesEndpoint(IDespesaRepository despesaRepository)
    {
        _despesaRepository = despesaRepository;
    }

    public override void Configure()
    {
        Get("usuarios/{idUsuario}/despesas");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListarDespesasDoMesRequest req, CancellationToken ct)
    {
        MesReferencia mesReferencia = (MesReferencia)(req.Mes, req.Ano);
        IReadOnlyList<Despesa> despesas = await _despesaRepository.ListarDoMesAsync(mesReferencia, ct);

        ListarDespesasDoMesResponse.Despesa[] despesasResponse = despesas
            .OrderBy(d => d.DataOcorrencia)
            .ThenBy(d => d.Descricao.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Ler)
            .ToArray();

        await Send.OkAsync(
            new ListarDespesasDoMesResponse(despesasResponse),
            ct);
    }

    /// <exception cref="InvalidOperationException">Quando a despesa não é inteira: a parcelada ainda não é gravada.</exception>
    private static ListarDespesasDoMesResponse.Despesa Ler(Despesa despesa)
    {
        if (despesa is not DespesaInteira inteira)
            throw new InvalidOperationException($"Despesa sem resposta mapeada: {despesa.GetType().Name}");

        return new(
            inteira.Id,
            inteira.IdBeneficiario,
            inteira.Descricao,
            inteira.IdCategoria,
            inteira.DataOcorrencia,
            inteira.FormaPagamento.Value,
            (inteira as DespesaAdiada)?.IdCartao,
            inteira.IdDespesaRecorrente,
            inteira.Pagamento.Valor,
            inteira.Pagamento.Status.Value,
            inteira.Pagamento.DataProjetada,
            inteira.Pagamento.DataEfetiva);
    }
}
