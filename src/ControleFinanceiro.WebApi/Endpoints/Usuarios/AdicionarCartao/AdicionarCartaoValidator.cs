using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

public sealed class AdicionarCartaoValidator : Validator<AdicionarCartaoRequest>
{
    public AdicionarCartaoValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.Banco)
            .MustBeSmartEnum(Banco.List);

        RuleFor(x => x.Sufixo)
            .MustBeValueObject(SufixoCartao.Criar);

        RuleFor(x => x.DiaVencimento)
            .MustBeValueObject(DiaVencimentoCartao.Criar);

        RuleFor(x => x.DiaFechamento)
            .MustBeValueObject(DiaFechamentoCartao.Criar);
    }
}
