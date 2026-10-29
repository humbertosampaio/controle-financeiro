using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.ListarDoMes;

public sealed class ListarDespesasDoMesValidator : Validator<ListarDespesasDoMesRequest>
{
    public ListarDespesasDoMesValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Mes)
            .MustBeMes();

        RuleFor(x => x.Ano)
            .MustBeAno();
    }
}
