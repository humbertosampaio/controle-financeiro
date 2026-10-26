using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;

namespace ControleFinanceiro.WebApi.Tests.Categorias.CadastrarLote;

/// <summary>
/// Monta o <see cref="ItemCategoriaDto"/> como uma categoria nova a partir de uma <see cref="CategoriaDespesa"/> do
/// fixture, com o nome e o alias dela, e o <see cref="CadastrarLoteCategoriasRequest"/> com alguns desses itens, de nomes e
/// aliases distintos. O <see cref="CadastrarLoteCategoriasRequest.IdUsuario"/> fica de fora: vem da rota, que vence o corpo.
/// </summary>
internal sealed class CadastrarLoteCategoriasRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((CategoriaDespesa categoria) => new ItemCategoriaDto
        {
            Nome = categoria.Nome,
            Alias = categoria.Aliases[0].Alias.TextoOriginal,
        });

        fixture.Register((ItemCategoriaDto[] itens) => new CadastrarLoteCategoriasRequest { Itens = itens });
    }
}
