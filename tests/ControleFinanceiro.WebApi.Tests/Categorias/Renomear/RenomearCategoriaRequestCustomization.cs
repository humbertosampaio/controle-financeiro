using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Categorias.Renomear;

namespace ControleFinanceiro.WebApi.Tests.Categorias.Renomear;

/// <summary>
/// Monta o <see cref="RenomearCategoriaRequest"/> com um <see cref="TextoObrigatorio"/> do fixture. Os ids ficam de
/// fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class RenomearCategoriaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((TextoObrigatorio nome) => new RenomearCategoriaRequest { Nome = nome });
    }
}
