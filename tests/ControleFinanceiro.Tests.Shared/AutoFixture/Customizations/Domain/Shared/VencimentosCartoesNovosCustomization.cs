using ControleFinanceiro.Domain.Cartoes;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

/// <summary>
/// Vencimentos só com o dia único, que vale para todo cartão novo. Com <c>[Frozen] DiaVencimentoCartao</c> no
/// teste, os cartões novos vencem nesse dia.
/// </summary>
internal sealed class VencimentosCartoesNovosCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DiaVencimentoCartao diaUnico) =>
            VencimentosCartoesNovos.Criar(diaUnico, porSufixo: []).Value);
    }
}
