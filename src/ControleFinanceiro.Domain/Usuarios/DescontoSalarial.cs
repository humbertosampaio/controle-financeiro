using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Usuarios;

[ValueObject]
public readonly partial struct IdDescontoSalarial
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class DescontoSalarial : Entity<IdDescontoSalarial>
{
    public TextoObrigatorio Descricao { get; private set; } = null!;
    public ReaisPositivo Valor { get; private set; } = null!;

    private DescontoSalarial()
    {
    }

    private DescontoSalarial(IdDescontoSalarial id, TextoObrigatorio descricao, ReaisPositivo valor)
        : base(id)
    {
        Descricao = descricao;
        Valor = valor;
    }

    public static DescontoSalarial Criar(TextoObrigatorio descricao, ReaisPositivo valor)
    {
        return new DescontoSalarial(IdDescontoSalarial.FromNewVersion7Guid(), descricao, valor);
    }
}
