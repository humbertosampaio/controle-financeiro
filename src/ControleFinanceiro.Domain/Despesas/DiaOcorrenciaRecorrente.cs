using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DiaOcorrenciaRecorrente : DiaValidoEmTodoMes
{
    private DiaOcorrenciaRecorrente(int value) : base(value)
    {
    }

    public static Result<DiaOcorrenciaRecorrente> Criar(int dia)
    {
        return Result.SuccessIf(
            EstaNoIntervalo(dia),
            value: new DiaOcorrenciaRecorrente(dia),
            $"O dia da ocorrência de uma despesa recorrente deve estar entre {Minimo} e {Maximo}. Valor fornecido: {dia}");
    }

    public static explicit operator DiaOcorrenciaRecorrente(int dia) => Criar(dia).Value;
}