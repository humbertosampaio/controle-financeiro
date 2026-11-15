using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O vencimento da fatura não fecha com as datas das compras dela, pela regra do <see cref="Fatura.ConferirVencimento"/>:
/// costuma ser a data digitada errada, ou o arquivo de outra fatura.
/// </summary>
public sealed class ErroVencimentoIncompativelComFatura : Erro
{
    public static readonly ErroVencimentoIncompativelComFatura Instancia = new();

    internal ErroVencimentoIncompativelComFatura()
        : base(
            "Importacoes.VencimentoIncompativelComFatura",
            $"O vencimento não fecha com as compras da fatura: nenhuma compra à vista pode ser do dia do vencimento ou depois, e a parcela de cada compra tem de vencer até {Fatura.MaximoMesesAteVencimento} meses depois do mês dela.")
    {
    }
}
