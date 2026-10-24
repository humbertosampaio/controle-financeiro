using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta. Que cada regra está ligada ao
/// campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class ImportarExtratoValidatorTests
{
    private readonly ImportarExtratoValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory]
    [InlineApiAutoData("")]
    [InlineApiAutoData("c6-conta")]
    [InlineApiAutoData("C6-CONTA-CSV")]
    public void Validate_Quando_LayoutDesconhecido_Deve_RetornarErroNoLayout(string layout, ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(requisicao with { Layout = layout });

        resultado.ShouldHaveValidationErrorFor(r => r.Layout);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_LayoutDeFatura_Deve_RetornarErroNoLayout(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(
            requisicao with { Layout = LayoutArquivoImportacao.C6CartaoCsv.Value });

        resultado.ShouldHaveValidationErrorFor(r => r.Layout);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemArquivo_Deve_RetornarErroNoArquivo(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(requisicao with { Arquivo = null });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoNoTamanhoMaximo_Deve_Passar(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoCom(ImportarExtratoValidator.TamanhoMaximoArquivo) });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ArquivoAcimaDoTamanhoMaximo_Deve_RetornarErroNoArquivo(ImportarExtratoRequest requisicao)
    {
        TestValidationResult<ImportarExtratoRequest> resultado = _sut.TestValidate(
            requisicao with { Arquivo = ArquivoCom(ImportarExtratoValidator.TamanhoMaximoArquivo + 1) });

        resultado.ShouldHaveValidationErrorFor(r => r.Arquivo);
    }

    // O validator só lê o tamanho declarado; o conteúdo não importa aqui.
    private static FormFile ArquivoCom(long tamanho) =>
        new(Stream.Null, baseStreamOffset: 0, tamanho, name: "arquivo", fileName: "extrato.csv");
}
