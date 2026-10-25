using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Traz qualquer inteiro para 1–28 e não repete o dia até usar os 28, como a
/// <see cref="DiaVencimentoCartaoCustomization"/>.
/// </summary>
internal sealed class DiaOcorrenciaRecorrenteCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new DistinctSpecimenBuilder<DiaOcorrenciaRecorrente>(
            new SpecimenFactory<int, DiaOcorrenciaRecorrente>(dia => (DiaOcorrenciaRecorrente)(dia % 28 + 1)),
            janela: DiaValidoEmTodoMes.Maximo - DiaValidoEmTodoMes.Minimo));
    }
}
