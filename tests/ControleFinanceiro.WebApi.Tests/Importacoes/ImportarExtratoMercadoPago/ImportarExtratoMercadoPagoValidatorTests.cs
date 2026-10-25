using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtratoMercadoPago;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtratoMercadoPago;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta. Que cada regra está ligada ao
/// campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class ImportarExtratoMercadoPagoValidatorTests
{
    private readonly ImportarExtratoMercadoPagoValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemArquivo_Deve_RetornarErroNoArquivo(ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(requisicao with { Arquivo = null });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoNoTamanhoMaximo_Deve_Passar(ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(ImportarExtratoMercadoPagoValidator.TamanhoMaximoArquivo) });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoAcimaDoTamanhoMaximo_Deve_RetornarErroNoArquivo(ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(ImportarExtratoMercadoPagoValidator.TamanhoMaximoArquivo + 1) });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory]
    [InlineApiAutoData(null)]
    [InlineApiAutoData("")]
    [InlineApiAutoData("123")]
    [InlineApiAutoData("12a4")]
    public void Validate_Quando_SufixoAusenteOuMalformado_Deve_RetornarErroNoSufixo(string? sufixo, ImportarExtratoMercadoPagoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoMercadoPagoRequest> resultado = _sut.TestValidate(requisicao with { SufixoConta = sufixo });

        resultado.ShouldHaveValidationErrorFor(r => r.SufixoConta);
    }
}
