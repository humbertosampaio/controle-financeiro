using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria as despesas adiadas das compras à vista que uma fatura trouxe sem despesa prevista. Cada uma nasce cobrada pela
/// compra e pendente. Só cria, nunca altera uma despesa existente: a prevista continua sem cobrança, para quem vincula
/// ligá-la à compra. Cada adiada prevista, pela regra do <see cref="DespesaAdiada.EhPrevisaoDe"/>, cobre uma compra só,
/// e a compra seguinte do mesmo beneficiário na mesma fatura ganha despesa.
/// </summary>
public static class DespesaAdiadaFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, entre eles o de cada compra.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, entre elas a de cada categoria do banco.</param>
    /// <param name="cartoesDoUsuario">Todos os cartões do usuário, entre eles o de cada compra.</param>
    /// <param name="adiadasDoUsuario">Todas as despesas adiadas do usuário, e só as dele.</param>
    /// <param name="transacoesNovas">
    /// As transações que a importação acabou de gravar, de todos os cartões do arquivo, na ordem dele: reimportar não cria
    /// a despesa de novo.
    /// </param>
    /// <returns>As despesas que faltam, na ordem em que as compras aparecem.</returns>
    /// <exception cref="ArgumentException">
    /// Quando um item das listas é de outro usuário, quando uma transação não é de cartão ou o cartão dela não está na
    /// lista, ou quando o beneficiário ou a categoria de uma compra não está na lista: a <see cref="LoteTransacoesFactory"/>
    /// já conferiu os aliases.
    /// </exception>
    public static IReadOnlyList<DespesaAdiada> CriarDaFatura(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        IReadOnlyCollection<DespesaAdiada> adiadasDoUsuario,
        IReadOnlyList<Transacao> transacoesNovas)
    {
        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));

        if (categoriasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma categoria de outro usuário.", nameof(categoriasDoUsuario));

        if (cartoesDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesDoUsuario));

        if (adiadasDoUsuario.Any(d => d.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(adiadasDoUsuario));

        if (transacoesNovas.Any(t => t.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma transação de outro usuário.", nameof(transacoesNovas));

        if (transacoesNovas.Any(t => t.EhOrigemConta))
            throw new ArgumentException("A lista traz uma transação que não é de fatura.", nameof(transacoesNovas));

        List<DespesaAdiada> previsoesLivres = adiadasDoUsuario.ToList();
        List<DespesaAdiada> criadas = [];

        foreach (Transacao compra in transacoesNovas.Where(t => t.EhCompraAVistaNoCartao))
        {
            // O Value é seguro: a lista já recusou a transação da conta.
            IdCartao idCartao = compra.Origem.IdCartao!.Value;
            IdBeneficiario idBeneficiario = AliasesResolvidos.IdBeneficiarioDo(beneficiariosDoUsuario, compra.AliasTextoBeneficiario!);
            FaturaCartao fatura = FaturaCartao.DaCompra(idCartao, compra.Data, cartoesDoUsuario);

            DespesaAdiada? previsao = previsoesLivres.FirstOrDefault(d => d.EhPrevisaoDe(fatura, idBeneficiario));
            if (previsao is not null)
            {
                previsoesLivres.Remove(previsao);
                continue;
            }

            criadas.Add(Criar(idUsuario, categoriasDoUsuario, idCartao, idBeneficiario, fatura, compra));
        }

        return criadas;
    }

    private static DespesaAdiada Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IdCartao idCartao,
        IdBeneficiario idBeneficiario,
        FaturaCartao fatura,
        Transacao compra)
    {
        return DespesaAdiada.CriarCobrada(
            idUsuario,
            idBeneficiario,
            compra.Descricao,
            AliasesResolvidos.IdCategoriaDo(categoriasDoUsuario, compra.AliasCategoriaBanco),
            compra.Data,
            idCartao,
            fatura.MesVencimento,
            fatura.DiaVencimento,
            new Cobranca(compra.Id, compra.ValorAbsoluto));
    }
}
