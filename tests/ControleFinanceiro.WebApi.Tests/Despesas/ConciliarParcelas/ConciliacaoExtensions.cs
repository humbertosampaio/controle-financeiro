using ControleFinanceiro.WebApi.Endpoints.Despesas.ConciliarParcelas;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.ConciliarParcelas;

/// <summary>A chamada da rota de conciliação, compartilhada pelos testes.</summary>
internal static class ConciliacaoExtensions
{
    extension(ApiComPostgresFixture api)
    {
        public Task<HttpResponseMessage> ConciliarAsync(Guid idUsuario)
        {
            return api.Client.PostAsync(
                new Uri($"/api/usuarios/{idUsuario}/despesas/conciliacoes", UriKind.Relative),
                content: null);
        }

        public async Task<ConciliarParcelasResponse> ConciliarComSucessoAsync(Guid idUsuario)
        {
            using HttpResponseMessage resposta = await api.ConciliarAsync(idUsuario);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());

            return (await resposta.Content.ReadFromJsonAsync<ConciliarParcelasResponse>())!;
        }
    }
}
