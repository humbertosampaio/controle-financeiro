using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;

/// <summary>
/// Liga as saídas parceladas sem vínculo às parcelas da mesma compra, inclusive as já gravadas. Chamar de novo não muda
/// o que já foi ligado.
/// </summary>
public sealed class ConciliarParcelasEndpoint : Endpoint<ConciliarParcelasRequest, ConciliarParcelasResponse>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConciliarParcelasEndpoint(
        IDespesaRepository despesaRepository,
        ITransacaoRepository transacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _despesaRepository = despesaRepository;
        _transacaoRepository = transacaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/despesas/conciliacoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ConciliarParcelasRequest req, CancellationToken ct)
    {
        IReadOnlyList<DespesaParcelada> parceladas = await _despesaRepository.ListarParceladasAsync(ct);
        IReadOnlyList<Transacao> transacoes = await _transacaoRepository.ListarParceladasAsync(ct);

        ConciliacaoParcelas conciliacao = ConciliacaoParcelasService.Conciliar(IdUsuario.From(req.IdUsuario), parceladas, transacoes);

        await _unitOfWork.CommitAsync(ct);

        await Send.OkAsync(new ConciliarParcelasResponse(conciliacao.Vinculadas, conciliacao.SemParcela), ct);
    }
}
