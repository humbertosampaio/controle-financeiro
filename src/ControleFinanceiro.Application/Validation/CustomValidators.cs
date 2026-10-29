using Ardalis.SmartEnum;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FluentValidation;
using Vogen;

namespace ControleFinanceiro.Application.Validation;

public static class CustomValidators
{
    extension<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, TProperty> MustBeEntity<TEntity, TEntityId>(
            Func<TProperty, Result<TEntity>> factoryMethod)
            where TEntity : Entity<TEntityId>
            where TEntityId : IComparable<TEntityId>
        {
            return ruleBuilder.Custom((property, context) =>
            {
                factoryMethod(property).TapError(error => context.AddFailure(error));
            });
        }

        public IRuleBuilderOptionsConditions<T, TProperty> MustBeValueObject<TValueObject>(
            Func<TProperty, Result<TValueObject>> factoryMethod)
            where TValueObject : ValueObject
        {
            return ruleBuilder.Custom((property, context) =>
            {
                factoryMethod(property).TapError(error => context.AddFailure(error));
            });
        }
    }

    extension<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, TProperty> MustBeId<TId>(
            Func<TProperty, ValueObjectOrError<TId>> tryFrom)
        {
            return ruleBuilder.Custom((property, context) =>
            {
                ValueObjectOrError<TId> resultado = tryFrom(property);
                if (!resultado.IsSuccess)
                    context.AddFailure(resultado.Error.ErrorMessage);
            });
        }
    }

    extension<T, TValue>(IRuleBuilder<T, TValue> ruleBuilder)
        where TValue : IEquatable<TValue>, IComparable<TValue>
    {
        public IRuleBuilderOptionsConditions<T, TValue> MustBeSmartEnum<TEnum>(IEnumerable<TEnum> conhecidos)
            where TEnum : SmartEnum<TEnum, TValue>
        {
            return ruleBuilder.Custom((property, context) =>
            {
                if (!conhecidos.Any(item => item.Value.Equals(property)))
                    context.AddFailure($"'{property}' não é um código de {typeof(TEnum).Name} conhecido");
            });
        }
    }

    extension<T>(IRuleBuilder<T, int> ruleBuilder)
    {
        /// <summary>O mês de um <see cref="MesReferencia"/>, de 1 a 12.</summary>
        public IRuleBuilderOptionsConditions<T, int> MustBeMes()
        {
            return ruleBuilder
                .MustBeValueObject(mes => MesReferencia.Criar(mes, ano: 1));
        }

        /// <summary>O ano de um <see cref="MesReferencia"/>, de 1 a 9999.</summary>
        public IRuleBuilderOptionsConditions<T, int> MustBeAno()
        {
            return ruleBuilder
                .MustBeValueObject(ano => MesReferencia.Criar(mes: 1, ano));
        }
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> MustBeTextoObrigatorio()
        {
            return ruleBuilder
                .MustBeValueObject(property => TextoObrigatorio.Criar(property));
        }
    }
}