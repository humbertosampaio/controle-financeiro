using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Calcula o balanço de um mês a partir da renda líquida atual do usuário e dos pagamentos das despesas que caem
/// nele, pelo valor vigente de cada um. A despesa do cartão cobrada e ainda não paga entra no pendente pelo valor
/// cobrado. A renda não tem histórico: um mês passado usa a de hoje, e entradas fora do salário não contam.
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

        if (despesasDoMes.Any(d => d.PagamentoDoMes(mesReferencia).HasNoValue))
            throw new ArgumentException("A lista traz uma despesa sem pagamento no mês.", nameof(despesasDoMes));

        bool EstaPaga(Despesa despesa) => despesa.PagamentoDoMes(mesReferencia).Value.EstaPago;
        ReaisPositivo ValorVigente(Despesa despesa) => despesa.ValorVigenteDoMes(mesReferencia).Value;

        return new BalancoMensal(
            mesReferencia,
            usuario.RendaLiquida,
            totalPago: despesasDoMes.Where(EstaPaga).Sum(ValorVigente),
            totalPendente: despesasDoMes.Where(d => !EstaPaga(d)).Sum(ValorVigente));
    }
}
