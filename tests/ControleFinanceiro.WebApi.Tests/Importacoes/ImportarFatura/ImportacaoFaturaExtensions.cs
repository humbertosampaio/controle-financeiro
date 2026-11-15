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
    public static Uri RotaFatura(Guid idUsuario, string layout) => new($"/api/usuarios/{idUsuario}/importacoes/faturas/{layout}", UriKind.Relative);

    /// <summary>O vencimento que o usuário informa no layout de fatura sem o vencimento no arquivo; nulo nos outros.</summary>
    public static DateOnly? VencimentoPadrao(LayoutArquivoImportacao layout) =>
        LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout) ? FaturaCartaoC6.Vencimento : null;

    /// <summary>O formulário da importação e da prévia da fatura, com o vencimento em ISO quando ele vem.</summary>
    public static MultipartFormDataContent FormularioFatura(byte[] arquivo, DateOnly? dataVencimento)
    {
        ByteArrayContent conteudo = new(arquivo);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        MultipartFormDataContent formulario = new() { { conteudo, "arquivo", "fatura.csv" } };
        if (dataVencimento is DateOnly data)
            formulario.Add(new StringContent(data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "dataVencimento");

        return formulario;
    }

    extension(ApiComPostgresFixture api)
    {
        /// <summary>A importação com o <see cref="VencimentoPadrao"/> do layout.</summary>
        public Task<HttpResponseMessage> ImportarFaturaAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            return api.ImportarFaturaAsync(idUsuario, layout, arquivo, VencimentoPadrao(layout));
        }

        public async Task<HttpResponseMessage> ImportarFaturaAsync(
            Guid idUsuario,
            LayoutArquivoImportacao layout,
            byte[] arquivo,
            DateOnly? dataVencimento)
        {
            using MultipartFormDataContent formulario = FormularioFatura(arquivo, dataVencimento);

            return await api.Client.PostAsync(RotaFatura(idUsuario, layout.Value), formulario);
        }

        public async Task<ImportarFaturaResponse> ImportarFaturaComSucessoAsync(Guid idUsuario, LayoutArquivoImportacao layout, byte[] arquivo)
        {
            using HttpResponseMessage resposta = await api.ImportarFaturaAsync(idUsuario, layout, arquivo);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
            return (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
        }

        /// <summary>A fatura fictícia do C6 importada por um usuário sem pendências, com a parcela 9/10 no final 1111.</summary>
        public async Task ImportarFaturaC6Async(Guid idUsuario)
        {
            LayoutArquivoImportacao layout = LayoutArquivoImportacao.C6CartaoCsv;
            byte[] arquivo = FaturaCartaoC6.Completo();
            await api.ResolverCartoesDaFaturaAsync(idUsuario, layout, arquivo);
            await api.ResolverBeneficiariosDaFaturaAsync(idUsuario, layout, arquivo);
            await api.ResolverCategoriasDaFaturaAsync(idUsuario, layout, arquivo);
            await api.ImportarFaturaComSucessoAsync(idUsuario, layout, arquivo);
        }

        public async Task DeveEstarSemImportacaoAsync()
        {
            (await api.ObterTodosAsync(db => db.Transacoes)).Should().BeEmpty();
            (await api.ObterTodosAsync(db => db.Importacoes)).Should().BeEmpty();
            (await api.ObterTodosAsync(db => db.Despesas)).Should().BeEmpty();
        }
    }
}
