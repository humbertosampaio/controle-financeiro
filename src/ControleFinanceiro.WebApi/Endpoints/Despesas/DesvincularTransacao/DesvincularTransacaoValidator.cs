using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesvincularTransacao;

public sealed class DesvincularTransacaoValidator : Validator<DesvincularTransacaoRequest>
{
    public DesvincularTransacaoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdDespesa)
            .MustBeId(IdDespesa.TryFrom);

        RuleFor(x => x.IdTransacao)
            .MustBeId(IdTransacao.TryFrom);
    }
}
