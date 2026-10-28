using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

[ValueObject]
public readonly partial struct IdDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public abstract class Despesa : AggregateRoot<IdDespesa>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; protected set; }
    public IdBeneficiario IdBeneficiario { get; protected set; }
    public TextoObrigatorio Descricao { get; protected set; }
    public IdCategoriaDespesa IdCategoria { get; protected set; }
    public DateOnly DataOcorrencia { get; protected set; }
    public FormaPagamento FormaPagamento { get; protected set; }

    protected Despesa(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdBeneficiario = idBeneficiario;
        Descricao = descricao;
        IdCategoria = idCategoria;
        DataOcorrencia = dataOcorrencia;
        FormaPagamento = formaPagamento;
    }
}
