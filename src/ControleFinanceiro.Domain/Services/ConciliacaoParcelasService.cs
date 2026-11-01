using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using static ControleFinanceiro.Domain.Services.EmparelhamentoParcelas;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Liga cada saída parcelada sem vínculo à parcela de mesmo número da mesma compra, reconhecida pela
/// <see cref="ChaveCompraParcelada"/>. Só vincula: não cria despesa, não altera transação e não toca nos vínculos
/// existentes. A idempotência vem do estado, porque só entram transação livre e parcela sem transação.
/// </summary>
public static class ConciliacaoParcelasService
{
    /// <param name="parceladas">Todas as despesas parceladas do usuário, e só as dele.</param>
    /// <param name="transacoesParceladas">Todas as transações com parcela do usuário, e só as dele.</param>
    /// <exception cref="ArgumentException">Quando um item das listas é de outro usuário.</exception>
    /// <exception cref="InvalidOperationException">Quando o vínculo falha depois do agrupamento: é bug.</exception>
    public static ConciliacaoParcelas Conciliar(
        IdUsuario idUsuario,
        IReadOnlyCollection<DespesaParcelada> parceladas,
        IReadOnlyCollection<Transacao> transacoesParceladas)
    {
        if (parceladas.Any(d => d.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(parceladas));

        if (transacoesParceladas.Any(t => t.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma transação de outro usuário.", nameof(transacoesParceladas));

        // Transação com parcela só paga parcela, então as parceladas bastam para saber quais já têm vínculo.
        HashSet<IdTransacao> jaVinculadas = parceladas.SelectMany(d => d.TransacoesVinculadas).ToHashSet();

        Transacao[] candidatas = transacoesParceladas
            .Where(t => t.EhParcelaPagaNoCartao && !jaVinculadas.Contains(t.Id))
            .ToArray();

        Vinculo[] vinculos = candidatas
            .GroupBy(ParcelaDaCompra.DaTransacao)
            .SelectMany(grupo => Emparelhar(grupo, VagasDe(parceladas, grupo.Key)))
            .ToArray();

        foreach (Vinculo vinculo in vinculos)
            Vincular(vinculo);

        return new ConciliacaoParcelas(vinculos.Length, candidatas.Length - vinculos.Length);
    }

    /// <summary>A parcela que nasceu paga também é vaga, porque recebe o valor real da transação.</summary>
    private static IEnumerable<Vaga> VagasDe(IEnumerable<DespesaParcelada> parceladas, ParcelaDaCompra parcela)
    {
        return parceladas
            .Where(d => d.EhDaCompra(parcela.Chave))
            .Select(d => new Vaga(d, d.ParcelaNumero(parcela.Numero)))
            .Where(vaga => !vaga.Parcela.TemTransacao);
    }

    private static void Vincular(Vinculo vinculo)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(vinculo.Transacao, vinculo.Despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        // O agrupamento já garante o que o Vincular confere. A mensagem leva só o código, sem dado da transação.
        if (result.IsFailure)
            throw new InvalidOperationException($"Vínculo recusado na conciliação das parcelas: {result.Error.Codigo}");
    }
}
