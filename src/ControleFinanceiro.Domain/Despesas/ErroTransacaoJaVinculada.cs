using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>A transação já paga a despesa <see cref="IdDespesa"/>, que pode ser a mesma do pedido.</summary>
public sealed class ErroTransacaoJaVinculada : Erro
{
    public IdDespesa IdDespesa { get; }

    internal ErroTransacaoJaVinculada(IdDespesa idDespesa)
        : base("Despesas.TransacaoJaVinculada", "A transação já paga uma despesa; desfaça o vínculo antes de trocar.")
    {
        IdDespesa = idDespesa;
    }
}
