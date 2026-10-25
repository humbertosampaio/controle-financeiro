using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Não repete o mês dentro de um ano de pedidos: o gerador de datas do AutoFixture alcança dois anos para cada
/// lado de hoje, então sempre sobra mês para sortear.
/// </summary>
internal sealed class MesReferenciaCustomization : ICustomization
{
    public void Customize(IFixture fixture) =>
        fixture.Customizations.Add(new DistinctSpecimenBuilder<MesReferencia>(new MesReferenciaGenerator(), janela: 12));
}
