using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria a despesa recorrente conferindo que o cartão, quando há um, é do usuário. A despesa guarda só o id
/// do cartão.
/// </summary>
public static class DespesaRecorrenteFactory
{
    /// <param name="cartao">Obrigatório no crédito e proibido nas outras formas de pagamento.</param>
    public static Result<DespesaRecorrente> Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        Cartao? cartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        return DespesaRecorrente
            .Criar(idUsuario, idBeneficiario, descricao, idCategoria, formaPagamento, cartao?.Id, valor, diaOcorrencia)
            .Ensure(
                _ => cartao is null || cartao.IdUsuario == idUsuario,
                "O cartão da despesa recorrente não pertence ao usuário.");
    }
}
