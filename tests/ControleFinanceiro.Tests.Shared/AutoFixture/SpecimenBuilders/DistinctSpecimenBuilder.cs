using AutoFixture.Kernel;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

public sealed class DistinctSpecimenBuilder<T> : ISpecimenBuilder
{
    /// <summary>
    /// Sorteios seguidos sem achar um valor fora da janela. Passar disso quer dizer que a janela cobre todos os
    /// valores que o gerador produz, e o laço não terminaria nunca.
    /// </summary>
    private const int MaximoTentativas = 10_000;

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
        int tentativas = 0;
        do
        {
            if (++tentativas > MaximoTentativas)
            {
                throw new InvalidOperationException(
                    $"Nenhum {typeof(T).Name} fora dos últimos {_janela} gerados em {MaximoTentativas} tentativas: " +
                    "a janela é grande demais para os valores que o gerador produz.");
            }

            specimen = (T)_specimenBuilder.Create(request, context);
        }
        while (_ultimos.Contains(specimen));

        _ultimos.Enqueue(specimen);
        if (_ultimos.Count > _janela)
            _ultimos.Dequeue();

        return specimen;
    }
}
