using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Liga a data de uma compra ao mês de vencimento da fatura em que ela cai, pelos dias do cartão. O dia de fechamento é
/// o último dia de compras da fatura, e o vencimento cai no mês do fechamento só quando o dia de vencimento é maior que
/// o de fechamento; com os dois dias iguais, vai para o mês seguinte.
/// </summary>
/// <remarks>
/// O banco empurra o fechamento e o vencimento que caem em fim de semana ou feriado, e o cálculo não sabe disso: a
/// compra feita nesses dias pode sair na fatura vizinha.
/// </remarks>
public static class VencimentoFaturaCartao
{
    public static MesReferencia MesVencimento(Cartao cartao, DateOnly dataCompra)
    {
        MesReferencia mesCompra = dataCompra;
        MesReferencia mesFechamento = dataCompra.Day <= cartao.DiaFechamento ? mesCompra : mesCompra.Posterior();

        return VenceNoMesDoFechamento(cartao) ? mesFechamento : mesFechamento.Posterior();
    }

    /// <summary>
    /// A inversa do <see cref="MesVencimento"/>: a data, no dia informado, da compra que cai na fatura que vence em
    /// <paramref name="mesVencimento"/>. Pode ficar até dois meses antes dele.
    /// </summary>
    public static DateOnly DataOcorrenciaQueVenceEm(Cartao cartao, MesReferencia mesVencimento, DiaValidoEmTodoMes dia)
    {
        MesReferencia mesFechamento = VenceNoMesDoFechamento(cartao) ? mesVencimento : mesVencimento.Anterior();
        MesReferencia mesCompra = dia <= cartao.DiaFechamento ? mesFechamento : mesFechamento.Anterior();

        return mesCompra.NoDia(dia);
    }

    private static bool VenceNoMesDoFechamento(Cartao cartao) => cartao.DiaVencimento > cartao.DiaFechamento;
}
