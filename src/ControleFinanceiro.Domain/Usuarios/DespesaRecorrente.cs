using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Usuarios;

[ValueObject]
public readonly partial struct IdDespesaRecorrente
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class DespesaRecorrente : Entity<IdDespesaRecorrente>
{
    public IdBeneficiario IdBeneficiario { get; private set; }
    public TextoObrigatorio Descricao { get; private set; }
    public IdCategoriaDespesa IdCategoria { get; private set; }
    public FormaPagamento FormaPagamento { get; private set; }
    public ReaisPositivo Valor { get; private set; }
    public DiaCompraRecorrente DiaCompra { get; private set; }

    private DespesaRecorrente(
        IdDespesaRecorrente id,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
            : base(id)
    {
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        FormaPagamento = formaPagamento;
        Valor = valor;
        DiaCompra = diaCompra;
    }

    public static DespesaRecorrente Criar(
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        FormaPagamento formaPagamento,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        return new(IdDespesaRecorrente.FromNewVersion7Guid(), idBeneficiario, descricao, idCategoria, formaPagamento, valor, diaCompra);
    }
}
