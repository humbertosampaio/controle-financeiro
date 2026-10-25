using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtratoMercadoPago;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtratoMercadoPago;

/// <summary>
/// Monta um <see cref="ImportarExtratoMercadoPagoRequest"/> válido para os testes do validator, com um sufixo gerado e
/// um arquivo de 1 byte: o validator só confere o tamanho, e o conteúdo é do leitor. O teste do endpoint manda o
/// formulário multipart direto, com o extrato fictício do Mercado Pago.
/// </summary>
internal sealed class ImportarExtratoMercadoPagoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Guid idUsuario, SufixoConta sufixoConta) => new ImportarExtratoMercadoPagoRequest
        {
            IdUsuario = idUsuario,
            Arquivo = ArquivoFormulario.ComTamanho(tamanho: 1),
            SufixoConta = sufixoConta.Value,
        });
    }
}
