using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta um <see cref="Cpf"/> válido: sorteia os 9 primeiros dígitos e tenta os 100 pares de verificadores até o
/// <c>Criar</c> aceitar um, sem repetir aqui o cálculo do domínio.
/// </summary>
public sealed class CpfGenerator : ISpecimenBuilder
{
    private const int QuantidadeDigitosBase = 9;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(Cpf))
            return new NoSpecimen();

        while (true)
        {
            string digitosBase = string.Concat(Enumerable
                .Range(start: 0, QuantidadeDigitosBase)
                .Select(_ => (context.Create<byte>() % 10).ToString(CultureInfo.InvariantCulture)));

            // Uma base com todos os caracteres iguais não tem par válido, e o laço sorteia outra.
            for (int verificadores = 0; verificadores < 100; verificadores++)
            {
                Result<Cpf> cpf = Cpf.Criar(string.Create(CultureInfo.InvariantCulture, $"{digitosBase}{verificadores:D2}"));
                if (cpf.IsSuccess)
                    return cpf.Value;
            }
        }
    }
}
