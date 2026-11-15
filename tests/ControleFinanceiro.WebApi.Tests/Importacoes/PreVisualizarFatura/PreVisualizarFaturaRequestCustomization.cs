using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.PreVisualizarFatura;

/// <summary>
/// Monta um <see cref="PreVisualizarFaturaRequest"/> válido para os testes do validator, com o layout da fatura do C6,
/// o vencimento que ele pede e um
/// arquivo de 1 byte: o validator só confere o tamanho, e o conteúdo é do leitor. O teste do endpoint manda o
/// formulário multipart direto, com as faturas fictícias.
/// </summary>
internal sealed class PreVisualizarFaturaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Guid idUsuario, DateOnly dataVencimento) => new PreVisualizarFaturaRequest
        {
            IdUsuario = idUsuario,
            Layout = LayoutArquivoImportacao.C6CartaoCsv.Value,
            Arquivo = ArquivoFormulario.ComTamanho(tamanho: 1),
            DataVencimento = dataVencimento,
        });
    }
}
