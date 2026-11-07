using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

/// <summary>
/// Registra que uma saída pagou uma despesa do usuário. Na parcelada, a parcela vem do número informado na transação,
/// e não da requisição.
/// </summary>
public sealed class VincularTransacaoEndpoint : Endpoint<VincularTransacaoRequest, VincularTransacaoResponse>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VincularTransacaoEndpoint(
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
        Post("usuarios/{idUsuario}/despesas/{idDespesa}/vinculos");
        AllowAnonymous();
    }

    public override async Task HandleAsync(VincularTransacaoRequest req, CancellationToken ct)
    {
        IdTransacao idTransacao = IdTransacao.From(req.IdTransacao);
        Maybe<Despesa> despesa = await _despesaRepository.ObterAsync(IdDespesa.From(req.IdDespesa), ct);
        Maybe<Transacao> transacao = await _transacaoRepository.ObterAsync(idTransacao, ct);
        if (despesa.HasNoValue || transacao.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        Maybe<Despesa> despesaQueUsaATransacao = await _despesaRepository.ObterQueUsaAsync(transacao.Value, ct);

        UnitResult<Erro> vinculo = VinculoTransacaoService.Vincular(transacao.Value, despesa.Value, despesaQueUsaATransacao);
        if (vinculo.IsFailure)
        {
            await EnviarErroAsync(vinculo.Error, ct);
            return;
        }

        await _unitOfWork.CommitAsync(ct);
        await Send.OkAsync(Ler(despesa.Value, idTransacao), ct);
    }

    /// <exception cref="InvalidOperationException">Quando o tipo da despesa não tem resposta mapeada.</exception>
    private static VincularTransacaoResponse Ler(Despesa despesa, IdTransacao idTransacao)
    {
        return despesa switch
        {
            DespesaInteira inteira => Ler(inteira.Id, parcela: null, inteira.Pagamento),
            // O Value é seguro: a transação acabou de cobrar uma parcela da despesa.
            DespesaParcelada parcelada => Ler(parcelada.Id, parcelada.ParcelaCobradaPor(idTransacao).Value),
            _ => throw new InvalidOperationException($"Despesa sem resposta mapeada: {despesa.GetType().Name}"),
        };
    }

    private static VincularTransacaoResponse Ler(IdDespesa idDespesa, ParcelaDespesa parcela) => Ler(idDespesa, parcela, parcela.Pagamento);

    private static VincularTransacaoResponse Ler(IdDespesa idDespesa, ParcelaDespesa? parcela, Pagamento pagamento)
    {
        return new(
            idDespesa.Value,
            parcela?.Id.Value,
            parcela?.NumeroParcela,
            pagamento.ValorProjetado,
            pagamento.ValorEfetivo!,
            pagamento.DataEfetiva!.Value);
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        AddError(r => r.IdTransacao, erro.Descricao);

        switch (erro)
        {
            case ErroTransacaoJaVinculada or ErroDespesaJaPaga:
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            case ErroTransacaoNaoEhSaida or ErroOrigemIncompativelComDespesa or ErroParcelaIncompativelComDespesa:
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
