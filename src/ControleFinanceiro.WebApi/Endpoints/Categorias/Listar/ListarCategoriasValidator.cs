using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Categorias.Listar;

public sealed class ListarCategoriasValidator : Validator<ListarCategoriasRequest>
{
    public ListarCategoriasValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);
    }
}
