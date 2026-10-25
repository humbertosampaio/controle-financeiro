using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Traz qualquer inteiro para 1–28 e não repete o dia até usar os 28: dois dias pedidos no mesmo teste, ou o
/// dia de um cartão do fixture e outro pedido à parte, saem sempre diferentes.
/// </summary>
internal sealed class DiaVencimentoCartaoCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new DistinctSpecimenBuilder<DiaVencimentoCartao>(
            new SpecimenFactory<int, DiaVencimentoCartao>(dia => (DiaVencimentoCartao)(dia % 28 + 1)),
            janela: DiaValidoEmTodoMes.Maximo - DiaValidoEmTodoMes.Minimo));
    }
}
