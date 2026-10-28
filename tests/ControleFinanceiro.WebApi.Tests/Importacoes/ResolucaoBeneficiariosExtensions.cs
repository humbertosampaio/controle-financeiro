using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarExtrato;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes;

/// <summary>
/// Resolve, pela prévia de verdade, os textos de beneficiário de um arquivo antes de importá-lo: cada texto pendente
/// vira um beneficiário novo, com o próprio texto como nome e alias. Sem pendentes, o lote não é enviado, porque o
/// validator recusa a lista vazia.
/// </summary>
internal static class ResolucaoBeneficiariosExtensions
{
    extension(ApiComPostgresFixture api)
    {
        public async Task ResolverBeneficiariosDoExtratoAsync(
            Guid idUsuario,
            LayoutArquivoImportacao layout,
            byte[] arquivo,
            string? sufixoConta = null)
        {
            using MultipartFormDataContent formulario = Formulario(arquivo, "extrato.csv");
            if (sufixoConta is not null)
                formulario.Add(new StringContent(sufixoConta), "sufixoConta");

            using HttpResponseMessage resposta = await api.Client.PostAsync(
                new Uri($"/api/usuarios/{idUsuario}/importacoes/extratos/{layout.Value}/previa", UriKind.Relative),
                formulario);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            PreVisualizarExtratoResponse previa = (await resposta.Content.ReadFromJsonAsync<PreVisualizarExtratoResponse>())!;

            await api.CadastrarPendentesAsync(idUsuario, previa.Beneficiarios);
        }

        public async Task ResolverBeneficiariosDaFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            using MultipartFormDataContent formulario = Formulario(arquivo, "fatura.csv");

            using HttpResponseMessage resposta = await api.Client.PostAsync(
                new Uri($"/api/usuarios/{idUsuario}/importacoes/faturas/{layout.Value}/previa", UriKind.Relative),
                formulario);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            PreVisualizarFaturaResponse previa = (await resposta.Content.ReadFromJsonAsync<PreVisualizarFaturaResponse>())!;

            await api.CadastrarPendentesAsync(idUsuario, previa.Beneficiarios);
        }

        private async Task CadastrarPendentesAsync(Guid idUsuario, IReadOnlyList<string> pendentes)
        {
            if (pendentes.Count == 0)
                return;

            await api.CadastrarBeneficiariosAsync(idUsuario, new CadastrarLoteBeneficiariosRequest
            {
                Itens = [.. pendentes.Select(texto => new ItemBeneficiarioDto { Alias = texto, Nome = texto })],
            });
        }
    }

    private static MultipartFormDataContent Formulario(byte[] arquivo, string nomeArquivo)
    {
        ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        return new MultipartFormDataContent { { conteudo, "arquivo", nomeArquivo } };
    }
}
