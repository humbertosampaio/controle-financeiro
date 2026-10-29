using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;

/// <summary>
/// Gera as despesas do mês a partir das recorrentes do usuário. Só cria as que faltam: chamar de novo para o mesmo mês
/// não duplica nada.
/// </summary>
public sealed class GerarDespesasDoMesEndpoint : Endpoint<GerarDespesasDoMesRequest, GerarDespesasDoMesResponse>
{
    private readonly IDespesaRecorrenteRepository _despesaRecorrenteRepository;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IDespesaRepository _despesaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GerarDespesasDoMesEndpoint(
        IDespesaRecorrenteRepository despesaRecorrenteRepository,
        ICartaoRepository cartaoRepository,
        IDespesaRepository despesaRepository,
        IUnitOfWork unitOfWork)
    {
        _despesaRecorrenteRepository = despesaRecorrenteRepository;
        _cartaoRepository = cartaoRepository;
        _despesaRepository = despesaRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/despesas/geracoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GerarDespesasDoMesRequest req, CancellationToken ct)
    {
        MesReferencia mesReferencia = (MesReferencia)(req.Mes, req.Ano);
        IReadOnlyList<DespesaRecorrente> recorrentes = await _despesaRecorrenteRepository.ListarAsync(ct);
        IReadOnlyList<Cartao> cartoes = await _cartaoRepository.ListarAsync(ct);
        IReadOnlyList<Despesa> despesasDoMes = await _despesaRepository.ListarDoMesAsync(mesReferencia, ct);

        // A geração só falharia com uma recorrente no crédito sem cartão, que a factory da recorrente não deixa nascer.
        IReadOnlyList<Despesa> geradas = GeracaoDespesasDoMesService
            .GerarParaMes(recorrentes, cartoes, despesasDoMes, mesReferencia)
            .Value;

        await _despesaRepository.AdicionarAsync(geradas, ct);
        await _unitOfWork.CommitAsync(ct);

        await Send.OkAsync(
            new GerarDespesasDoMesResponse(geradas.Count, recorrentes.Count - geradas.Count),
            ct);
    }
}
