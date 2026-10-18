using AutoFixture.Kernel;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public sealed class DistinctSpecimenBuilder<T> : ISpecimenBuilder
{
    private readonly int _janela;
    private readonly Queue<T> _ultimos = new();
    private readonly ISpecimenBuilder _specimenBuilder;

    public DistinctSpecimenBuilder(ISpecimenBuilder specimenBuilder, int janela)
    {
        _specimenBuilder = specimenBuilder;
        _janela = janela;
    }

    public object Create(object request, ISpecimenContext context)
    {
        if (!typeof(T).Equals(request))
            return new NoSpecimen();

        T specimen;
        do
        {
            specimen = (T)_specimenBuilder.Create(request, context);
        }
        while (_ultimos.Contains(specimen));

        _ultimos.Enqueue(specimen);
        if (_ultimos.Count > _janela)
            _ultimos.Dequeue();

        return specimen;
    }
}
