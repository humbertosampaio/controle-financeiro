using ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AlterarCicloFaturaCartao;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio. Os dias de vencimento
/// e de fechamento só delegam aos value objects, já testados no domínio; que cada regra está ligada ao campo certo, o teste de integração
/// do endpoint confere.
/// </summary>
public sealed class AlterarCicloFaturaCartaoValidatorTests
{
    private readonly AlterarCicloFaturaCartaoValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(
        Guid idUsuario,
        Guid idCartao,
        AlterarCicloFaturaCartaoRequest requisicao)
    {
        TestValidationResult<AlterarCicloFaturaCartaoRequest> resultado =
            _sut.TestValidate(requisicao with { IdUsuario = idUsuario, IdCartao = idCartao });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(AlterarCicloFaturaCartaoRequest requisicao)
    {
        TestValidationResult<AlterarCicloFaturaCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdCartaoVazio_Deve_RetornarErroNoIdCartao(AlterarCicloFaturaCartaoRequest requisicao)
    {
        TestValidationResult<AlterarCicloFaturaCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdCartao = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdCartao);
    }
}
