namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>De que arquivo veio o lançamento: o extrato de uma conta ou a fatura de um cartão.</summary>
public enum TipoOrigem
{
    Conta,
    Cartao,
}
