using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesfazerPagamentoFatura;

/// <summary>
/// Desfaz o pagamento da fatura pela saída da conta: as despesas do cartão e as parcelas que ela pagou voltam a pendente,
/// com a cobrança preservada. A despesa simples paga pela mesma saída fica como está: ela se desfaz pelo desvínculo.
/// </summary>
public sealed class DesfazerPagamentoFaturaEndpoint : Endpoint<DesfazerPagamentoFaturaRequest>
{
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesfazerPagamentoFaturaEndpoint(
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
        Delete("usuarios/{idUsuario}/despesas/pagamentos-fatura/{idTransacao}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(DesfazerPagamentoFaturaRequest req, CancellationToken ct)
    {
        IdTransacao idTransacao = IdTransacao.From(req.IdTransacao);
        Maybe<Transacao> saida = await _transacaoRepository.ObterAsync(idTransacao, ct);
        if (saida.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        IReadOnlyList<Despesa> despesasPagasPelaSaida = await _despesaRepository.ListarPeloPagamentoAsync(idTransacao, ct);

        await PagamentoFaturaService
            .Desfazer(saida.Value, despesasPagasPelaSaida)
            .Tap(() => _unitOfWork.CommitAsync(ct))
            .Match(
                () => Send.NoContentAsync(ct),
                erro => EnviarErroAsync(erro, ct));
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroTransacaoNaoPagouADespesa:
                await Send.NotFoundAsync(ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
