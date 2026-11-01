using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>A importação do extrato pela rota, para os testes de outras áreas que partem de transações de conta.</summary>
internal static class ImportacaoExtratoExtensions
{
    extension(ApiComPostgresFixture api)
    {
        /// <summary>O extrato fictício do C6 importado por um usuário sem pendências.</summary>
        public async Task ImportarExtratoC6Async(Guid idUsuario)
        {
            LayoutArquivoImportacao layout = LayoutArquivoImportacao.C6ContaCsv;
            byte[] arquivo = ExtratoContaC6.Completo();
            await api.ResolverBeneficiariosDoExtratoAsync(idUsuario, layout, arquivo);

            using ByteArrayContent conteudo = new(arquivo);
            conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            using MultipartFormDataContent formulario = new()
            {
                { conteudo, "arquivo", "extrato.csv" },
            };

            using HttpResponseMessage resposta = await api.Client.PostAsync(
                new Uri($"/api/usuarios/{idUsuario}/importacoes/extratos/{layout.Value}", UriKind.Relative),
                formulario);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        }
    }
}
