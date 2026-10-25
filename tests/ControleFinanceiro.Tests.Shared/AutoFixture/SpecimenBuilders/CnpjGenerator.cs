using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta um <see cref="Cnpj"/> alfanumérico válido: sorteia os 12 primeiros caracteres entre <c>0</c>–<c>9</c> e
/// <c>A</c>–<c>Z</c> e tenta os 100 pares de verificadores até o <c>Criar</c> aceitar um, sem repetir aqui o cálculo
/// do domínio.
/// </summary>
public sealed class CnpjGenerator : ISpecimenBuilder
{
    private const int QuantidadeCaracteresBase = 12;
    private const string Alfabeto = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Cnpj))
            return new NoSpecimen();

        while (true)
        {
            string caracteresBase = string.Concat(Enumerable
                .Range(start: 0, QuantidadeCaracteresBase)
                .Select(_ => Alfabeto[context.Create<byte>() % Alfabeto.Length]));

            // Uma base com todos os caracteres iguais não tem par válido, e o laço sorteia outra.
            for (int verificadores = 0; verificadores < 100; verificadores++)
            {
                Result<Cnpj> cnpj = Cnpj.Criar(string.Create(CultureInfo.InvariantCulture, $"{caracteresBase}{verificadores:D2}"));
                if (cnpj.IsSuccess)
                    return cnpj.Value;
            }
        }
    }
}
