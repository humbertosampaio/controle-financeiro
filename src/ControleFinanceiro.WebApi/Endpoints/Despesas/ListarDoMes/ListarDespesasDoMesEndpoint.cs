using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

/// <summary>
/// As despesas do usuário num mês de referência, geradas ou não por uma recorrente. A parcelada sai com os dados da
/// parcela daquele mês.
/// </summary>
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
            .Select(despesa => Ler(despesa, mesReferencia))
            .ToArray();

        await Send.OkAsync(
            new ListarDespesasDoMesResponse(despesasResponse),
            ct);
    }

    /// <exception cref="InvalidOperationException">Quando o tipo da despesa não tem resposta mapeada.</exception>
    private static ListarDespesasDoMesResponse.Despesa Ler(Despesa despesa, MesReferencia mesReferencia)
    {
        return despesa switch
        {
            DespesaInteira inteira => Ler(inteira),
            DespesaParcelada parcelada => Ler(parcelada, parcelada.ParcelaDoMes(mesReferencia).Value),
            _ => throw new InvalidOperationException($"Despesa sem resposta mapeada: {despesa.GetType().Name}"),
        };
    }

    private static ListarDespesasDoMesResponse.Despesa Ler(DespesaInteira inteira)
    {
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
            inteira.Pagamento.DataEfetiva,
            Parcela: null);
    }

    private static ListarDespesasDoMesResponse.Despesa Ler(DespesaParcelada parcelada, ParcelaDespesa parcela)
    {
        return new(
            parcelada.Id,
            parcelada.IdBeneficiario,
            parcelada.Descricao,
            parcelada.IdCategoria,
            parcelada.DataOcorrencia,
            parcelada.FormaPagamento.Value,
            parcelada.IdCartao,
            parcelada.IdDespesaRecorrente,
            parcela.Pagamento.Valor,
            parcela.Pagamento.Status.Value,
            parcela.Pagamento.DataProjetada,
            parcela.Pagamento.DataEfetiva,
            new ListarDespesasDoMesResponse.ParcelaDoMes(parcela.Id, parcela.NumeroParcela, parcelada.Parcelas.Count));
    }
}
