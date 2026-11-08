using ControleFinanceiro.Application.Validation;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;

public sealed class PagarFaturaValidator : Validator<PagarFaturaRequest>
{
    public PagarFaturaValidator()
    {
        RuleFor(x => x.IdUsuario)
            .MustBeId(IdUsuario.TryFrom);

        RuleFor(x => x.IdTransacao)
            .MustBeId(IdTransacao.TryFrom);

        RuleFor(x => x.IdCartao)
            .MustBeId(IdCartao.TryFrom);

        RuleFor(x => x.Mes)
            .MustBeMes();

        RuleFor(x => x.Ano)
            .MustBeAno();
    }
}
