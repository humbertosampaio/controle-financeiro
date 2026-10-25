using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarVencimentoCartao;

public sealed class AlterarVencimentoCartaoValidator : Validator<AlterarVencimentoCartaoRequest>
{
    public AlterarVencimentoCartaoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdCartao)
            .MustBeId(IdCartao.TryFrom);

        RuleFor(x => x.DiaVencimento)
            .MustBeValueObject(DiaVencimentoCartao.Criar);
    }
}
