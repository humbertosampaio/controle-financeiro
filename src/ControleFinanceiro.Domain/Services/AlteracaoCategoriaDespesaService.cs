using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;

namespace ControleFinanceiro.Domain.Services;

/// <summary>Troca a categoria de uma despesa, conferindo que a despesa e a categoria são do mesmo usuário.</summary>
public static class AlteracaoCategoriaDespesaService
{
    /// <exception cref="ArgumentException">Quando a categoria é de outro usuário: é bug de quem chama.</exception>
    public static void Alterar(Despesa despesa, CategoriaDespesa categoria)
    {
        if (categoria.IdUsuario != despesa.IdUsuario)
            throw new ArgumentException("A categoria é de outro usuário.", nameof(categoria));

        despesa.AlterarCategoria(categoria.Id);
    }
}
