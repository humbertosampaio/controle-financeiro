using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesfazerPagamentoFatura;

public sealed class DesfazerPagamentoFaturaValidator : Validator<DesfazerPagamentoFaturaRequest>
{
    public DesfazerPagamentoFaturaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdTransacao)
            .MustBeId(IdTransacao.TryFrom);
    }
}
