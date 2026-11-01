using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

namespace ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;

/// <summary>
/// Monta o <see cref="VincularTransacaoRequest"/> com um <see cref="IdTransacao"/> do fixture, que não existe no banco. Os
/// outros ids ficam de fora: vêm da rota, que vence o corpo.
/// </summary>
internal sealed class VincularTransacaoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((IdTransacao idTransacao) => new VincularTransacaoRequest { IdTransacao = idTransacao.Value });
    }
}
