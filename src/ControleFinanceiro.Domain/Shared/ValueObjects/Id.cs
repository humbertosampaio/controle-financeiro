namespace ControleFinanceiro.Domain.Shared.ValueObjects;

public static class Id
{
    public static Validation Validar(Guid input)
    {
        return input != Guid.Empty
            ? Validation.Ok
            : Validation.Invalid("Id não pode ser vazio");
    }
}
