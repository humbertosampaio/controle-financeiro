using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// O pagamento da fatura que o modelo pede, já como tipo de saída, em duas etapas, como no
/// <see cref="VinculoParaModeloService"/>. A regra é a do <see cref="PagamentoFaturaService.Pagar"/>, aplicada em memória
/// nas duas: a prévia nunca grava, e a confirmação, chamada depois que o usuário aprova o resumo no cliente, refaz o
/// pagamento do zero e só grava se o resumo recalculado for o que ele aprovou. Os repositórios enxergam só o usuário
/// atual, então transação ou cartão de outro usuário é tratado como inexistente.
/// </summary>
public sealed class PagamentoFaturaParaModeloService
{
    private readonly ITransacaoRepository _transacoes;
    private readonly IDespesaRepository _despesas;
    private readonly ICartaoRepository _cartoes;
    private readonly IUnitOfWork _unitOfWork;

    public PagamentoFaturaParaModeloService(
        ITransacaoRepository transacoes,
        IDespesaRepository despesas,
        ICartaoRepository cartoes,
        IUnitOfWork unitOfWork)
    {
        _transacoes = transacoes;
        _despesas = despesas;
        _cartoes = cartoes;
        _unitOfWork = unitOfWork;
    }

    /// <param name="idTransacao">Vem do modelo: um id que não é de transação vira erro, e não exceção.</param>
    /// <param name="idCartao">Vem do modelo: um id que não é de cartão vira erro, e não exceção.</param>
    /// <param name="ano">O ano do vencimento da fatura; inválido, vira erro.</param>
    /// <param name="mes">O mês do vencimento da fatura; inválido, vira erro.</param>
    public async Task<PagamentoFaturaParaModelo> PreverAsync(
        Guid idTransacao,
        Guid idCartao,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        return await AplicarAsync(idTransacao, idCartao, ano, mes, cancellationToken)
            .Match(previa => previa, PagamentoFaturaParaModelo.Falha);
    }

    /// <param name="idTransacao">Vem do modelo: um id que não é de transação vira erro, e não exceção.</param>
    /// <param name="idCartao">Vem do modelo: um id que não é de cartão vira erro, e não exceção.</param>
    /// <param name="ano">O ano do vencimento da fatura; inválido, vira erro.</param>
    /// <param name="mes">O mês do vencimento da fatura; inválido, vira erro.</param>
    /// <param name="resumo">O resumo da prévia que o usuário aprovou, nos argumentos da chamada.</param>
    public async Task<PagamentoFaturaParaModelo> ConfirmarAsync(
        Guid idTransacao,
        Guid idCartao,
        int ano,
        int mes,
        string resumo,
        CancellationToken cancellationToken)
    {
        return await AplicarAsync(idTransacao, idCartao, ano, mes, cancellationToken)
            .Ensure(pagamento => pagamento.ConfereResumo(resumo), ErroParaModelo.ResumoPagamentoFaturaDivergente)
            .Tap(() => _unitOfWork.CommitAsync(cancellationToken))
            .Match(pagamento => pagamento, PagamentoFaturaParaModelo.Falha);
    }

    private async Task<Result<PagamentoFaturaParaModelo, ErroParaModelo>> AplicarAsync(
        Guid idTransacao,
        Guid idCartao,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesValido = MesReferencia.Criar(mes, ano);
        if (mesValido.IsFailure)
            return ErroParaModelo.MesInvalido;

        Maybe<Transacao> saida = await ObterTransacaoAsync(idTransacao, cancellationToken);
        if (saida.HasNoValue)
            return ErroParaModelo.TransacaoNaoEncontrada;

        Maybe<Cartao> cartao = await ObterCartaoAsync(idCartao, cancellationToken);
        if (cartao.HasNoValue)
            return ErroParaModelo.CartaoNaoEncontrado;

        MesReferencia mesVencimento = mesValido.Value;
        IReadOnlyList<Cartao> cartoesDoUsuario = await _cartoes.ListarAsync(cancellationToken);
        IReadOnlyList<Despesa> despesasDoMes = await _despesas.ListarDoMesAsync(mesVencimento, cancellationToken);
        IReadOnlyList<Despesa> despesasPagasPelaSaida = await _despesas.ListarPeloPagamentoAsync(saida.Value.Id, cancellationToken);

        return PagamentoFaturaService
            .Pagar(saida.Value, cartao.Value, mesVencimento, cartoesDoUsuario, despesasDoMes, despesasPagasPelaSaida)
            .MapError(ErroParaModelo.De)
            .Map(pagamento => PagamentoFaturaParaModelo.De(saida.Value, cartao.Value, pagamento));
    }

    private async Task<Maybe<Transacao>> ObterTransacaoAsync(Guid idTransacao, CancellationToken cancellationToken)
    {
        if (!IdTransacao.TryFrom(idTransacao, out IdTransacao id))
            return Maybe<Transacao>.None;

        return await _transacoes.ObterAsync(id, cancellationToken);
    }

    private async Task<Maybe<Cartao>> ObterCartaoAsync(Guid idCartao, CancellationToken cancellationToken)
    {
        if (!IdCartao.TryFrom(idCartao, out IdCartao id))
            return Maybe<Cartao>.None;

        return await _cartoes.ObterAsync(id, cancellationToken);
    }
}
