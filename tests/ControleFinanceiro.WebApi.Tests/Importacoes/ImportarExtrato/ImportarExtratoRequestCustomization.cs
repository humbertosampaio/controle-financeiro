using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;
using Microsoft.AspNetCore.Http;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Monta um <see cref="ImportarExtratoRequest"/> válido para os testes do validator, com o layout do extrato (o da
/// fatura é recusado) e um arquivo de 1 byte:
/// o validator só confere o tamanho, e o conteúdo é do leitor. O teste do endpoint manda o formulário multipart
/// direto, com o extrato fictício do C6.
/// </summary>
internal sealed class ImportarExtratoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Guid idUsuario) => new ImportarExtratoRequest
        {
            IdUsuario = idUsuario,
            Layout = LayoutArquivoImportacao.C6ContaCsv.Value,
            Arquivo = new FormFile(Stream.Null, baseStreamOffset: 0, length: 1, name: "arquivo", fileName: "extrato.csv"),
        });
    }
}
