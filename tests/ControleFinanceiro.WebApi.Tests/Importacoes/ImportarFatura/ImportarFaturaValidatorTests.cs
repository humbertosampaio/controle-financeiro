using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta. Que cada regra está ligada ao
/// campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class ImportarFaturaValidatorTests
{
    private readonly ImportarFaturaValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemDiaVencimentoNemVencimentos_Deve_Passar(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { DiaVencimento = null, Vencimentos = null });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory]
    [InlineApiAutoData("")]
    [InlineApiAutoData("c6-cartao")]
    [InlineApiAutoData("c6-conta-csv")]
    public void Validate_Quando_LayoutQueNaoEDeFatura_Deve_RetornarErroNoLayout(string layout, ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(requisicao with { Layout = layout });

        resultado.ShouldHaveValidationErrorFor(r => r.Layout);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemArquivo_Deve_RetornarErroNoArquivo(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(requisicao with { Arquivo = null });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoNoTamanhoMaximo_Deve_Passar(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(ImportarFaturaValidator.TamanhoMaximoArquivo) });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoAcimaDoTamanhoMaximo_Deve_RetornarErroNoArquivo(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoFormulario.ComTamanho(ImportarFaturaValidator.TamanhoMaximoArquivo + 1) });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory]
    [InlineApiAutoData(0)]
    [InlineApiAutoData(29)]
    public void Validate_Quando_DiaVencimentoForaDe1A28_Deve_RetornarErroNoDiaVencimento(int dia, ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(requisicao with { DiaVencimento = dia });

        resultado.ShouldHaveValidationErrorFor(r => r.DiaVencimento);
    }

    [Theory]
    [InlineApiAutoData("123", 10)]
    [InlineApiAutoData("12A4", 10)]
    [InlineApiAutoData("1234", 0)]
    [InlineApiAutoData("1234", 29)]
    public void Validate_Quando_VencimentoComFinalOuDiaInvalido_Deve_RetornarErroNosVencimentos(
        string sufixo,
        int dia,
        ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Vencimentos = new Dictionary<string, int> { [sufixo] = dia } });

        resultado.ShouldHaveValidationErrorFor(r => r.Vencimentos);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_VencimentosRepetemOFinalComEspaco_Deve_RetornarErroNosVencimentos(ImportarFaturaRequest requisicao)
    {
        TestValidationResult<ImportarFaturaRequest> resultado = _sut.TestValidate(
            requisicao with { Vencimentos = new Dictionary<string, int> { ["1234"] = 10, [" 1234"] = 20 } });

        resultado.ShouldHaveValidationErrorFor(r => r.Vencimentos);
    }
}
