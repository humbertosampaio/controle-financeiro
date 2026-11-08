using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;

/// <summary>
/// Paga a fatura do cartão com uma saída da conta: as despesas cobradas e pendentes dos cartões da mesma fatura, no mês
/// de vencimento, ficam pagas com a data da saída e o valor cobrado. Chamar de novo com a mesma saída e a mesma fatura
/// paga as cobranças que chegaram depois.
/// </summary>
public sealed class PagarFaturaEndpoint : Endpoint<PagarFaturaRequest, PagarFaturaResponse>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PagarFaturaEndpoint(
        IDespesaRepository despesaRepository,
        ITransacaoRepository transacaoRepository,
        ICartaoRepository cartaoRepository,
        IUnitOfWork unitOfWork)
    {
        _despesaRepository = despesaRepository;
        _transacaoRepository = transacaoRepository;
        _cartaoRepository = cartaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/despesas/pagamentos-fatura");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PagarFaturaRequest req, CancellationToken ct)
    {
        IdTransacao idTransacao = IdTransacao.From(req.IdTransacao);
        Maybe<Transacao> saida = await _transacaoRepository.ObterAsync(idTransacao, ct);
        Maybe<Cartao> cartao = await _cartaoRepository.ObterAsync(IdCartao.From(req.IdCartao), ct);
        if (saida.HasNoValue || cartao.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        MesReferencia mesVencimento = (MesReferencia)(req.Mes, req.Ano);
        IReadOnlyList<Cartao> cartoes = await _cartaoRepository.ListarAsync(ct);
        IReadOnlyList<Despesa> despesasDoMes = await _despesaRepository.ListarDoMesAsync(mesVencimento, ct);
        IReadOnlyList<Despesa> despesasPagasPelaSaida = await _despesaRepository.ListarPeloPagamentoAsync(idTransacao, ct);

        await PagamentoFaturaService
            .Pagar(saida.Value, cartao.Value, mesVencimento, cartoes, despesasDoMes, despesasPagasPelaSaida)
            .Tap(() => _unitOfWork.CommitAsync(ct))
            .Match(
                pagamento => Send.OkAsync(Ler(pagamento, mesVencimento), ct),
                erro => EnviarErroAsync(erro, ct));
    }

    private static PagarFaturaResponse Ler(PagamentoFatura pagamento, MesReferencia mesVencimento)
    {
        return new PagarFaturaResponse(pagamento.Pagas.Select(d => Ler(d, mesVencimento)).ToList());
    }

    /// <exception cref="InvalidOperationException">Quando o tipo da despesa não tem resposta mapeada.</exception>
    private static PagarFaturaResponse.DespesaPaga Ler(Despesa despesa, MesReferencia mesVencimento)
    {
        return despesa switch
        {
            DespesaAdiada adiada => Ler(adiada.Id, adiada.Pagamento, parcela: null),
            // O Value é seguro: a saída acabou de pagar a parcela do mês.
            DespesaParcelada parcelada => Ler(parcelada, parcelada.ParcelaDoMes(mesVencimento).Value),
            _ => throw new InvalidOperationException($"Despesa sem resposta mapeada: {despesa.GetType().Name}"),
        };
    }

    private static PagarFaturaResponse.DespesaPaga Ler(DespesaParcelada parcelada, ParcelaDespesa parcela)
    {
        return Ler(
            parcelada.Id,
            parcela.Pagamento,
            new PagarFaturaResponse.ParcelaPaga(parcela.Id.Value, parcela.NumeroParcela, parcelada.Parcelas.Count));
    }

    /// <exception cref="InvalidOperationException">Quando o pagamento não está pago: é bug do serviço.</exception>
    private static PagarFaturaResponse.DespesaPaga Ler(IdDespesa idDespesa, Pagamento pagamento, PagarFaturaResponse.ParcelaPaga? parcela)
    {
        if (!pagamento.EstaPago)
            throw new InvalidOperationException("O pagamento da fatura devolveu uma despesa pendente.");

        return new(idDespesa.Value, pagamento.ValorEfetivo, pagamento.DataEfetiva.Value, parcela);
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroTransacaoJaVinculada or ErroTransacaoPagaOutraFatura:
                AddError(r => r.IdTransacao, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            case ErroTransacaoNaoEhSaida or ErroOrigemIncompativelComDespesa:
                AddError(r => r.IdTransacao, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case ErroFaturaSemCobrancaPendente:
                AddError(r => r.IdCartao, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
