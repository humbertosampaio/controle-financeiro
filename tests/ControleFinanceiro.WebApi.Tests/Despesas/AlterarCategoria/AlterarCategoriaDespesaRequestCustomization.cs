using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.WebApi.Endpoints.Despesas.AlterarCategoria;

namespace ControleFinanceiro.WebApi.Tests.Despesas.AlterarCategoria;

/// <summary>
/// Monta o <see cref="AlterarCategoriaDespesaRequest"/> com um <see cref="IdCategoriaDespesa"/> do fixture. Os outros
/// ids ficam de fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class AlterarCategoriaDespesaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((IdCategoriaDespesa idCategoria) => new AlterarCategoriaDespesaRequest { IdCategoria = idCategoria.Value });
    }
}
