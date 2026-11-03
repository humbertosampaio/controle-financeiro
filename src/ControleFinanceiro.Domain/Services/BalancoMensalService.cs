using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Calcula o balanço de um mês a partir da renda líquida atual do usuário e dos pagamentos das despesas que caem
/// nele. A renda não tem histórico: um mês passado usa a de hoje, e entradas fora do salário não contam.
/// </summary>
public static class BalancoMensalService
{
    /// <param name="despesasDoMes">As despesas do usuário com pagamento no mês, inclusive a parcelada com parcela nele.</param>
    /// <exception cref="ArgumentException">
    /// Quando uma despesa da lista é de outro usuário ou não tem pagamento no mês.
    /// </exception>
    public static BalancoMensal Calcular(Usuario usuario, IReadOnlyCollection<Despesa> despesasDoMes, MesReferencia mesReferencia)
    {
        if (despesasDoMes.Any(d => d.IdUsuario != usuario.Id))
            throw new ArgumentException("A lista traz uma despesa de outro usuário.", nameof(despesasDoMes));

        Maybe<Pagamento>[] pagamentosDoMes = despesasDoMes
            .Select(d => d.PagamentoDoMes(mesReferencia))
            .ToArray();

        if (pagamentosDoMes.Any(p => p.HasNoValue))
            throw new ArgumentException("A lista traz uma despesa sem pagamento no mês.", nameof(despesasDoMes));

        Pagamento[] pagamentos = pagamentosDoMes
            .Select(p => p.Value)
            .ToArray();

        return new BalancoMensal(
            mesReferencia,
            usuario.RendaLiquida,
            totalPago: pagamentos.Where(p => p.EstaPago).Sum(p => p.ValorVigente),
            totalPendente: pagamentos.Where(p => !p.EstaPago).Sum(p => p.ValorVigente));
    }
}
