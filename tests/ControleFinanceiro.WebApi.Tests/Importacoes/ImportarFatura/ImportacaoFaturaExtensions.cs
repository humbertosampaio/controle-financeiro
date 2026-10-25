using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// A importação da fatura pela rota, compartilhada pelos testes de cada layout de fatura: todos usam o mesmo endpoint.
/// </summary>
internal static class ImportacaoFaturaExtensions
{
    public static Uri RotaFatura(Guid idUsuario) => new($"/api/usuarios/{idUsuario}/importacoes/faturas", UriKind.Relative);

    extension(ApiComPostgresFixture api)
    {
        public async Task<HttpResponseMessage> ImportarFaturaAsync(
            Guid idUsuario,
            LayoutArquivoImportacao layout,
            byte[] arquivo,
            int? diaVencimento = null,
            string? vencimentos = null)
        {
            using ByteArrayContent conteudo = new(arquivo);
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

            using MultipartFormDataContent formulario = new()
            {
                { new StringContent(layout.Value), "layout" },
                { conteudo, "arquivo", "fatura.csv" },
            };

            if (diaVencimento is int dia)
                formulario.Add(new StringContent(dia.ToString(CultureInfo.InvariantCulture)), "diaVencimento");

            if (vencimentos is not null)
                formulario.Add(new StringContent(vencimentos), "vencimentos");

            return await api.Client.PostAsync(RotaFatura(idUsuario), formulario);
        }

        public async Task<ImportarFaturaResponse> ImportarFaturaComSucessoAsync(
            Guid idUsuario,
            LayoutArquivoImportacao layout,
            byte[] arquivo,
            int? diaVencimento = null,
            string? vencimentos = null)
        {
            using HttpResponseMessage resposta = await api.ImportarFaturaAsync(idUsuario, layout, arquivo, diaVencimento, vencimentos);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            return (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
        }

        public async Task DeveEstarSemImportacaoAsync()
        {
            (await api.ObterTodosAsync(db => db.Cartoes)).Should().BeEmpty();
            (await api.ObterTodosAsync(db => db.Transacoes)).Should().BeEmpty();
            (await api.ObterTodosAsync(db => db.Importacoes)).Should().BeEmpty();
        }
    }
}
