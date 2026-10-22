namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Um dia do mês que existe em todo mês, inclusive em fevereiro: de 1 a 28. Cada tipo derivado valida o
/// dia com a própria mensagem de erro, e o <see cref="MesReferencia"/> monta a data sem precisar falhar.
/// </summary>
public abstract class DiaValidoEmTodoMes : SimpleValueObject<int>
{
    public const int Minimo = 1;
    public const int Maximo = 28;

    protected DiaValidoEmTodoMes(int value) : base(value)
    {
    }

    protected static bool EstaNoIntervalo(int dia) => dia is >= Minimo and <= Maximo;
}
