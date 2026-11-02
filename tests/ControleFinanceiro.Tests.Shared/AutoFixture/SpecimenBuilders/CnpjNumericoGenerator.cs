using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using System.Globalization;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>Monta um <see cref="CnpjNumerico"/> como o <see cref="CnpjGenerator"/> monta o alfanumérico, só com dígitos.</summary>
public sealed class CnpjNumericoGenerator : ISpecimenBuilder
{
    private const int QuantidadeDigitosBase = 12;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(CnpjNumerico))
            return new NoSpecimen();

        while (true)
        {
            string digitosBase = string.Concat(Enumerable
                .Range(start: 0, QuantidadeDigitosBase)
                .Select(_ => (char)('0' + (context.Create<byte>() % 10))));

            // Uma base com todos os dígitos iguais não tem par válido, e o laço sorteia outra.
            for (int verificadores = 0; verificadores < 100; verificadores++)
            {
                Result<Cnpj> cnpj = Cnpj.Criar(string.Create(CultureInfo.InvariantCulture, $"{digitosBase}{verificadores:D2}"));
                if (cnpj.IsSuccess)
                    return new CnpjNumerico(cnpj.Value);
            }
        }
    }
}
