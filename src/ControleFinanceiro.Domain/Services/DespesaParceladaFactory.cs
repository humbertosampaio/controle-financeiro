using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria as despesas parceladas que as compras de uma fatura revelam e que o usuário ainda não tem. Só cria, nunca altera
/// uma despesa existente. A mesma compra é reconhecida pela <see cref="ChaveCompraParcelada"/>. Compras gêmeas, com a
/// mesma chave e o mesmo número na mesma fatura, viram uma despesa cada.
/// </summary>
public static class DespesaParceladaFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, entre eles o de cada compra.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, entre elas a de cada categoria do banco.</param>
    /// <param name="fatura">
    /// A fatura do arquivo, de onde saem os lançamentos do cartão, na ordem dele, e o vencimento em que a parcela informada
    /// vence: os dois vêm da mesma fatura.
    /// </param>
    /// <param name="parceladasDoUsuario">Todas as despesas parceladas do usuário, e só as dele.</param>
    /// <returns>As despesas que faltam, na ordem em que as compras aparecem no arquivo.</returns>
    /// <exception cref="ArgumentException">
    /// Quando o cartão ou um item das listas é de outro usuário, quando a fatura é de outro banco ou não traz o final do
    /// cartão, ou quando o beneficiário ou a categoria de uma compra não está na lista:
    /// a <see cref="LoteTransacoesCartaoFactory"/> já conferiu os aliases.
    /// </exception>
    public static IReadOnlyList<DespesaParcelada> CriarDaFatura(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        Cartao cartao,
        Fatura fatura,
        IReadOnlyCollection<DespesaParcelada> parceladasDoUsuario)
    {
        if (cartao.IdUsuario != idUsuario)
            throw new ArgumentException("O cartão é de outro usuário.", nameof(cartao));

        if (fatura.Banco != cartao.Banco)
            throw new ArgumentException("A fatura é de outro banco.", nameof(fatura));

        if (!fatura.Sufixos.Contains(cartao.Sufixo))
            throw new ArgumentException("A fatura não traz o final do cartão.", nameof(fatura));

        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));

        if (categoriasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma categoria de outro usuário.", nameof(categoriasDoUsuario));

        if (parceladasDoUsuario.Any(d => d.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(parceladasDoUsuario));

        return fatura
            .LancamentosCartao(cartao.Sufixo)
            .Where(l => l.EhCompraParcelada)
            .GroupBy(l => ChaveCompraParcelada.DoLancamento(cartao.Id, l))
            .SelectMany(compra => LinhasSemDespesa(compra, compra.Key, parceladasDoUsuario))
            .Select(lancamento => Criar(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, cartao, fatura.Vencimento, lancamento))
            .ToArray();
    }

    /// <summary>
    /// As linhas do número que mais aparece na compra, descontadas as que já têm despesa. No empate fica o menor número,
    /// porque ele só marca como pagas as parcelas que certamente vieram antes.
    /// </summary>
    private static IEnumerable<LancamentoCartao> LinhasSemDespesa(
        IEnumerable<LancamentoCartao> compra,
        ChaveCompraParcelada chave,
        IReadOnlyCollection<DespesaParcelada> parceladasDoUsuario)
    {
        int existentes = parceladasDoUsuario.Count(d => d.EhDaCompra(chave));

        return compra
            .GroupBy(l => l.Parcela!.Numero)
            .OrderByDescending(numero => numero.Count())
            .ThenBy(numero => numero.Key)
            .First()
            .Skip(existentes);
    }

    private static DespesaParcelada Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        Cartao cartao,
        VencimentoFatura vencimento,
        LancamentoCartao lancamento)
    {
        return DespesaParcelada.Criar(
            idUsuario,
            AliasesResolvidos.IdBeneficiarioDo(beneficiariosDoUsuario, lancamento.AliasTextoBeneficiario!),
            lancamento.Descricao,
            AliasesResolvidos.IdCategoriaDo(categoriasDoUsuario, lancamento.AliasCategoriaBanco),
            lancamento.Data,
            lancamento.ValorAbsoluto,
            cartao.Id,
            vencimento,
            cartao.DiaVencimento,
            lancamento.Parcela!);
    }
}
