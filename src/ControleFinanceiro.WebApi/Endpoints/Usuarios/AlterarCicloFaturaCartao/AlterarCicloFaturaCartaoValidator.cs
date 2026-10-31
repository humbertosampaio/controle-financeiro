using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;

public sealed class AlterarCicloFaturaCartaoValidator : Validator<AlterarCicloFaturaCartaoRequest>
{
    public AlterarCicloFaturaCartaoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdCartao)
            .MustBeId(IdCartao.TryFrom);

        RuleFor(x => x.DiaVencimento)
            .MustBeValueObject(DiaVencimentoCartao.Criar);

        RuleFor(x => x.DiaFechamento)
            .MustBeValueObject(DiaFechamentoCartao.Criar);
    }
}
