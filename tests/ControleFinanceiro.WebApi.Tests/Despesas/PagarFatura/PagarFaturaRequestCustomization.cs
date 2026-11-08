using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;

namespace ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;

/// <summary>
/// Monta o <see cref="PagarFaturaRequest"/> com um <see cref="IdTransacao"/> e um <see cref="IdCartao"/> do fixture, que
/// não existem no banco, e o mês de um <see cref="MesReferencia"/>. O <see cref="PagarFaturaRequest.IdUsuario"/> fica de
/// fora: vem da rota, que vence o corpo.
/// </summary>
internal sealed class PagarFaturaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((IdTransacao idTransacao, IdCartao idCartao, MesReferencia mesVencimento) => new PagarFaturaRequest
        {
            IdTransacao = idTransacao.Value,
            IdCartao = idCartao.Value,
            Ano = mesVencimento.Ano,
            Mes = mesVencimento.Mes,
        });
    }
}
