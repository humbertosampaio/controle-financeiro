using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.PreVisualizarFatura;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta. Que cada regra está ligada ao
/// campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class PreVisualizarFaturaValidatorTests
{
    private readonly PreVisualizarFaturaValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_LayoutDaFaturaDoMercadoPago_Deve_Passar(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Layout = LayoutArquivoImportacao.MercadoPagoCartaoCsv.Value });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineApiAutoData("")]
    [InlineApiAutoData("c6-conta-csv")]
    [InlineApiAutoData("C6-CARTAO-CSV")]
    public void Validate_Quando_LayoutQueNaoEDeFatura_Deve_RetornarErroNoLayout(string layout, PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(requisicao with { Layout = layout });

        resultado.ShouldHaveValidationErrorFor(r => r.Layout);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemArquivo_Deve_RetornarErroNoArquivo(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(requisicao with { Arquivo = null });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoNoTamanhoMaximo_Deve_Passar(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(PreVisualizarFaturaValidator.TamanhoMaximoArquivo) });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoAcimaDoTamanhoMaximo_Deve_RetornarErroNoArquivo(PreVisualizarFaturaRequest requisicao)
    {
        TestValidationResult<PreVisualizarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(PreVisualizarFaturaValidator.TamanhoMaximoArquivo + 1) });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }
}
