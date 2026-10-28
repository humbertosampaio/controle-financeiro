using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaParcelada : Despesa
{
    public IReadOnlyList<ParcelaDespesa> Parcelas { get; private set; }

    public DespesaParcelada(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateOnly dataOcorrencia,
        IEnumerable<ParcelaDespesa> parcelas)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito)
    {
        Parcelas = parcelas.ToArray();
    }
}
