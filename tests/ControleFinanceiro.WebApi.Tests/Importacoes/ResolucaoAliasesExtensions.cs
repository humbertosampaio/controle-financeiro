using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarExtrato;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes;

/// <summary>
/// Resolve, pela prévia de verdade, os finais de cartão, os textos de beneficiário e as categorias do banco de um
/// arquivo antes de importá-lo: cada final sem cartão vira um cartão, e cada texto pendente vira um beneficiário ou uma
/// categoria nova, com o próprio texto como nome e alias. Sem pendentes, o lote não é enviado, porque o validator
/// recusa a lista vazia.
/// </summary>
internal static class ResolucaoAliasesExtensions
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

        public async Task ResolverCartoesDaFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            PreVisualizarFaturaResponse previa = await api.PreVisualizarFaturaAsync(idUsuario, layout, arquivo);
            Banco banco = layout == LayoutArquivoImportacao.C6CartaoCsv ? Banco.C6 : Banco.MercadoPago;

            foreach (PreVisualizarFaturaResponse.FinalCartao final in previa.FinaisCartao.Where(f => !f.Cadastrado))
            {
                await api.AdicionarCartaoAsync(idUsuario, new AdicionarCartaoRequest
                {
                    Banco = banco,
                    Sufixo = final.Sufixo,
                    DiaVencimento = 10,
                    DiaFechamento = 3,
                });
            }
        }

        public async Task ResolverBeneficiariosDaFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            PreVisualizarFaturaResponse previa = await api.PreVisualizarFaturaAsync(idUsuario, layout, arquivo);

            await api.CadastrarPendentesAsync(idUsuario, previa.Beneficiarios);
        }

        public Task ResolverCategoriasDaFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            return api.CadastrarCategoriasPendentesAsync(
                idUsuario,
                layout,
                arquivo,
                texto => new ItemCategoriaDto { Alias = texto, Nome = texto });
        }

        /// <summary>Associa cada categoria do banco pendente a uma categoria existente do usuário, como a Outros.</summary>
        public Task AssociarCategoriasDaFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo, Guid idCategoria)
        {
            return api.CadastrarCategoriasPendentesAsync(
                idUsuario,
                layout,
                arquivo,
                texto => new ItemCategoriaDto { Alias = texto, IdCategoria = idCategoria });
        }

        private async Task CadastrarCategoriasPendentesAsync(
            Guid idUsuario,
            LayoutArquivoImportacao layout,
            byte[] arquivo,
            Func<string, ItemCategoriaDto> item)
        {
            PreVisualizarFaturaResponse previa = await api.PreVisualizarFaturaAsync(idUsuario, layout, arquivo);
            if (previa.CategoriasBanco.Count == 0)
                return;

            await api.CadastrarCategoriasAsync(idUsuario, new CadastrarLoteCategoriasRequest
            {
                Itens = previa.CategoriasBanco
                    .Select(item)
                    .ToArray(),
            });
        }

        private async Task<PreVisualizarFaturaResponse> PreVisualizarFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            using MultipartFormDataContent formulario = Formulario(arquivo, "fatura.csv");

            using HttpResponseMessage resposta = await api.Client.PostAsync(
                new Uri($"/api/usuarios/{idUsuario}/importacoes/faturas/{layout.Value}/previa", UriKind.Relative),
                formulario);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            return (await resposta.Content.ReadFromJsonAsync<PreVisualizarFaturaResponse>())!;
        }

        private async Task CadastrarPendentesAsync(Guid idUsuario, IReadOnlyList<string> pendentes)
        {
            if (pendentes.Count == 0)
                return;

            await api.CadastrarBeneficiariosAsync(idUsuario, new CadastrarLoteBeneficiariosRequest
            {
                Itens = pendentes
                    .Select(texto => new ItemBeneficiarioDto { Alias = texto, Nome = texto })
                    .ToArray(),
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
