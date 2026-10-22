using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

public sealed class AdicionarCartaoMapper : Mapper<AdicionarCartaoRequest, AdicionarCartaoResponse, Cartao>
{
    public override Cartao ToEntity(AdicionarCartaoRequest r)
    {
        return Cartao.Criar(
            Banco.FromValue(r.Banco),
            (SufixoCartao)r.Sufixo,
            (DiaVencimentoCartao)r.DiaVencimento);
    }

    public override AdicionarCartaoResponse FromEntity(Cartao e) => new(e.Id);
}
