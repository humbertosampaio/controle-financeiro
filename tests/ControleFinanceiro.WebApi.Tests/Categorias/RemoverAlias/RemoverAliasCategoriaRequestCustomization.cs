using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Categorias.RemoverAlias;

namespace ControleFinanceiro.WebApi.Tests.Categorias.RemoverAlias;

/// <summary>
/// Monta o <see cref="RemoverAliasCategoriaRequest"/> com um <see cref="Alias"/> do fixture, que nenhuma categoria tem.
/// Os ids ficam de fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class RemoverAliasCategoriaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Alias alias) => new RemoverAliasCategoriaRequest { Alias = alias.TextoOriginal });
    }
}
