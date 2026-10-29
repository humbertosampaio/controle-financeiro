using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;

namespace ControleFinanceiro.WebApi.Tests.Despesas.GerarDoMes;

/// <summary>
/// Monta o <see cref="GerarDespesasDoMesRequest"/> a partir de um <see cref="MesReferencia"/> do fixture. Com
/// <c>[Frozen] MesReferencia</c> no teste, a requisição pede o mesmo mês que o teste espera encontrar no banco. O
/// <see cref="GerarDespesasDoMesRequest.IdUsuario"/> fica de fora: vem da rota, que vence o corpo.
/// </summary>
internal sealed class GerarDespesasDoMesRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((MesReferencia mesReferencia) => new GerarDespesasDoMesRequest
        {
            Ano = mesReferencia.Ano,
            Mes = mesReferencia.Mes,
        });
    }
}
