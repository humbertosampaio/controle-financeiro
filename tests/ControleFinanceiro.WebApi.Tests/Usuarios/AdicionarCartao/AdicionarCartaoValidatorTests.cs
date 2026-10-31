using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AdicionarCartao;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio. Sufixo e dias de
/// vencimento e de fechamento só delegam aos value objects, já testados no domínio; que cada regra está ligada ao campo
/// certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class AdicionarCartaoValidatorTests
{
    private readonly AdicionarCartaoValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(Guid idUsuario, AdicionarCartaoRequest requisicao)
    {
        TestValidationResult<AdicionarCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(AdicionarCartaoRequest requisicao)
    {
        TestValidationResult<AdicionarCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_BancoConhecido_Deve_AceitarOBanco(Banco banco, AdicionarCartaoRequest requisicao)
    {
        TestValidationResult<AdicionarCartaoRequest> resultado = _sut.TestValidate(requisicao with { Banco = banco });

        resultado.ShouldNotHaveValidationErrorFor(r => r.Banco);
    }

    // O código é comparado como veio: aparar ou completar com zeros é papel do adaptador de cada extrato.
    [Theory]
    [InlineApiAutoData(null)]
    [InlineApiAutoData("")]
    [InlineApiAutoData("999")]
    [InlineApiAutoData("36")]
    [InlineApiAutoData("0336")]
    [InlineApiAutoData(" 336")]
    public void Validate_Quando_BancoDesconhecido_Deve_RetornarErroNoBanco(string? banco, AdicionarCartaoRequest requisicao)
    {
        TestValidationResult<AdicionarCartaoRequest> resultado = _sut.TestValidate(requisicao with { Banco = banco! });

        resultado.ShouldHaveValidationErrorFor(r => r.Banco);
    }
}
