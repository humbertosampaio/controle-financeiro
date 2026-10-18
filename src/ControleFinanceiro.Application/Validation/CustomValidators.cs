using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FluentValidation;

namespace ControleFinanceiro.Application.Validation;

public static class CustomValidators
{
    extension<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder)
    {
        public IRuleBuilder<T, TProperty> MustBeEntity<TEntity, TEntityId>(
            Func<TProperty, Result<TEntity>> factoryMethod)
            where TEntity : Entity<TEntityId>
            where TEntityId : IComparable<TEntityId>
        {
            return ruleBuilder.Custom((property, context) =>
            {
                factoryMethod(property).TapError(error => context.AddFailure(error));
            });
        }

        public IRuleBuilder<T, TProperty> MustBeValueObject<TValueObject>(
            Func<TProperty, Result<TValueObject>> factoryMethod)
            where TValueObject : ValueObject
        {
            return ruleBuilder.Custom((property, context) =>
            {
                factoryMethod(property).TapError(error => context.AddFailure(error));
            });
        }
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilder<T, string?> MustBeTextoObrigatorio()
        {
            return ruleBuilder
                .MustBeValueObject(property => TextoObrigatorio.Criar(property));
        }
    }
}