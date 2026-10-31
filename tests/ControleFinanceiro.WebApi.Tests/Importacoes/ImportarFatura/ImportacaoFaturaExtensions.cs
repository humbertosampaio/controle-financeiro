using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// A importação da fatura pela rota, compartilhada pelos testes de cada layout de fatura: todos usam o mesmo endpoint.
/// </summary>
internal static class ImportacaoFaturaExtensions
{
    public static Uri RotaFatura(Guid idUsuario, string layout) => new($"/api/usuarios/{idUsuario}/importacoes/faturas/{layout}", UriKind.Relative);

    extension(ApiComPostgresFixture api)
    {
        public async Task<HttpResponseMessage> ImportarFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            using ByteArrayContent conteudo = new(arquivo);
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

            using MultipartFormDataContent formulario = new()
            {
                { conteudo, "arquivo", "fatura.csv" },
            };

            return await api.Client.PostAsync(RotaFatura(idUsuario, layout.Value), formulario);
        }

        public async Task<ImportarFaturaResponse> ImportarFaturaComSucessoAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            using HttpResponseMessage resposta = await api.ImportarFaturaAsync(idUsuario, layout, arquivo);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            return (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
        }

        public async Task DeveEstarSemImportacaoAsync()
        {
            (await api.ObterTodosAsync(db => db.Transacoes)).Should().BeEmpty();
            (await api.ObterTodosAsync(db => db.Importacoes)).Should().BeEmpty();
        }
    }
}
