using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria as despesas parceladas que as compras de uma fatura revelam e que o usuário ainda não tem. Só cria, nunca altera
/// uma despesa existente. A mesma compra é reconhecida pelo cartão, pela data, pela descrição normalizada e pelo total de
/// parcelas: o valor fica de fora porque parcelas da mesma compra podem ter valores diferentes. Compras gêmeas, com a
/// mesma chave e o mesmo número na mesma fatura, viram uma despesa cada.
/// </summary>
public static class DespesaParceladaFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, entre eles o de cada compra.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, entre elas a de cada categoria do banco.</param>
    /// <param name="parceladasDoUsuario">Todas as despesas parceladas do usuário, e só as dele.</param>
    /// <param name="lancamentos">Os lançamentos da fatura, na ordem do arquivo.</param>
    /// <returns>As despesas que faltam, na ordem em que as compras aparecem no arquivo.</returns>
    /// <exception cref="ArgumentException">
    /// Quando o cartão ou um item das listas é de outro usuário, ou quando o beneficiário ou a categoria de uma compra não
    /// está na lista: a <see cref="LoteTransacoesFactory"/> já conferiu os aliases.
    /// </exception>
    public static IReadOnlyList<DespesaParcelada> CriarDaFatura(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        Cartao cartao,
        IReadOnlyCollection<DespesaParcelada> parceladasDoUsuario,
        IReadOnlyList<Lancamento> lancamentos)
    {
        if (cartao.IdUsuario != idUsuario)
            throw new ArgumentException("O cartão é de outro usuário.", nameof(cartao));

        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));

        if (categoriasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma categoria de outro usuário.", nameof(categoriasDoUsuario));

        if (parceladasDoUsuario.Any(d => d.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(parceladasDoUsuario));

        return lancamentos
            .Where(EhCompraParcelada)
            .GroupBy(l => (l.Data, Descricao: (TextoNormalizado)l.Descricao, l.Parcela!.Total))
            .SelectMany(compra => LinhasSemDespesa(compra, compra.Key.Descricao, cartao, parceladasDoUsuario))
            .Select(lancamento => Criar(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, cartao, lancamento))
            .ToArray();
    }

    /// <summary>A linha sem texto de beneficiário fica de fora porque a despesa exige um beneficiário.</summary>
    private static bool EhCompraParcelada(Lancamento lancamento)
    {
        return lancamento.EhSaida
            && lancamento.Parcela is not null
            && Alias.Criar(lancamento.TextoBeneficiario(TipoOrigem.Cartao)).IsSuccess;
    }

    /// <summary>
    /// As linhas do número que mais aparece na compra, descontadas as que já têm despesa. No empate fica o menor número,
    /// porque ele só marca como pagas as parcelas que certamente vieram antes.
    /// </summary>
    private static IEnumerable<Lancamento> LinhasSemDespesa(
        IEnumerable<Lancamento> compra,
        TextoNormalizado descricao,
        Cartao cartao,
        IReadOnlyCollection<DespesaParcelada> parceladasDoUsuario)
    {
        Lancamento[] linhas = compra.ToArray();
        DateOnly data = linhas[0].Data;
        int total = linhas[0].Parcela!.Total;

        int existentes = parceladasDoUsuario.Count(d =>
            d.IdCartao == cartao.Id
            && d.DataOcorrencia == data
            && (TextoNormalizado)d.Descricao == descricao
            && d.Parcelas.Count == total);

        return linhas
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
        Lancamento lancamento)
    {
        return DespesaParcelada.Criar(
            idUsuario,
            IdBeneficiarioDa(beneficiariosDoUsuario, lancamento),
            lancamento.Descricao,
            IdCategoriaDa(categoriasDoUsuario, lancamento),
            lancamento.Data,
            (ReaisPositivo)decimal.Abs(lancamento.Valor),
            cartao.Id,
            VencimentoFaturaCartao.MesVencimento(cartao, lancamento.Data),
            cartao.DiaVencimento,
            lancamento.Parcela!);
    }

    private static IdBeneficiario IdBeneficiarioDa(IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario, Lancamento lancamento)
    {
        Alias alias = Alias.Criar(lancamento.TextoBeneficiario(TipoOrigem.Cartao)).Value;
        Beneficiario beneficiario = beneficiariosDoUsuario.FirstOrDefault(b => b.PossuiAlias(alias))
            ?? throw new ArgumentException("O beneficiário de uma compra não está na lista.", nameof(beneficiariosDoUsuario));

        return beneficiario.Id;
    }

    /// <returns>Nulo quando o banco não dá categoria à compra.</returns>
    private static IdCategoriaDespesa? IdCategoriaDa(IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario, Lancamento lancamento)
    {
        Result<Alias> alias = Alias.Criar(lancamento.CategoriaBanco);
        if (alias.IsFailure)
            return null;

        CategoriaDespesa categoria = categoriasDoUsuario.FirstOrDefault(c => c.PossuiAlias(alias.Value))
            ?? throw new ArgumentException("A categoria de uma compra não está na lista.", nameof(categoriasDoUsuario));

        return categoria.Id;
    }
}
