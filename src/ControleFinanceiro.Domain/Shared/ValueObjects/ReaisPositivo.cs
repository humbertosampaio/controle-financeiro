namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Representa um valor monetário positivo em Reais (BRL)
/// </summary>
public sealed class ReaisPositivo : Reais
{
    private ReaisPositivo(decimal valor) : base(valor)
    {
    }

    public static Result<ReaisPositivo> Criar(Reais reais) => Criar(reais.Value);

    public static Result<ReaisPositivo> Criar(decimal valor)
    {
        return Result.Success(valor)
            .Ensure(
                v => v >= 0,
                v =>
                {
                    FormattableString erro = $"O valor deve ser positivo. Encontrado: {v}";
                    return erro.ToString(FormatProvider);
                })
            .Map(v => new ReaisPositivo(v));
    }

    public static explicit operator ReaisPositivo(decimal valor) => Criar(valor).Value;
}
