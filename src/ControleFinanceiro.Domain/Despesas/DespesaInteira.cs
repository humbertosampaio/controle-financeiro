using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public abstract class DespesaInteira : Despesa
{
    public MesReferencia MesReferencia { get; protected set; }
    public Pagamento Pagamento { get; protected set; }

    protected DespesaInteira(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        MesReferencia mesReferencia,
        Pagamento pagamento)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento)
    {
        MesReferencia = mesReferencia;
        Pagamento = pagamento;
    }
}
