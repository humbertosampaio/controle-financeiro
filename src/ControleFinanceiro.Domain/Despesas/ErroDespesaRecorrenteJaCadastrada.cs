using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// O usuário já tem a despesa recorrente <see cref="IdDespesaRecorrente"/>, do mesmo beneficiário e com a mesma
/// descrição, sem contar caixa, acentos e espaços repetidos.
/// </summary>
public sealed class ErroDespesaRecorrenteJaCadastrada : Erro
{
    public IdDespesaRecorrente IdDespesaRecorrente { get; }

    internal ErroDespesaRecorrenteJaCadastrada(IdDespesaRecorrente idDespesaRecorrente)
        : base("DespesasRecorrentes.JaCadastrada", "O usuário já tem uma despesa recorrente desse beneficiário com essa descrição.")
    {
        IdDespesaRecorrente = idDespesaRecorrente;
    }
}
