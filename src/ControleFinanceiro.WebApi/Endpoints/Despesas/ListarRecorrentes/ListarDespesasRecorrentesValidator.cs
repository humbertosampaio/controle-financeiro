using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarRecorrentes;

public sealed class ListarDespesasRecorrentesValidator : Validator<ListarDespesasRecorrentesRequest>
{
    public ListarDespesasRecorrentesValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);
    }
}
