using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;

public sealed class GerarDespesasDoMesValidator : Validator<GerarDespesasDoMesRequest>
{
    public GerarDespesasDoMesValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Mes)
            .MustBeMes();

        RuleFor(x => x.Ano)
            .MustBeAno();
    }
}
