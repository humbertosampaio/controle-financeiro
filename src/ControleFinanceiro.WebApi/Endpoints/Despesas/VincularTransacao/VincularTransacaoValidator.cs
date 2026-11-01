using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

public sealed class VincularTransacaoValidator : Validator<VincularTransacaoRequest>
{
    public VincularTransacaoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdDespesa)
            .MustBeId(IdDespesa.TryFrom);

        RuleFor(x => x.IdTransacao)
            .MustBeId(IdTransacao.TryFrom);
    }
}
