using ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarVencimentoCartao;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AlterarVencimentoCartao;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio. O dia de vencimento só
/// delega ao value object, já testado no domínio; que cada regra está ligada ao campo certo, o teste de integração
/// do endpoint confere.
/// </summary>
public sealed class AlterarVencimentoCartaoValidatorTests
{
    private readonly AlterarVencimentoCartaoValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(
        Guid idUsuario,
        Guid idCartao,
        AlterarVencimentoCartaoRequest requisicao)
    {
        TestValidationResult<AlterarVencimentoCartaoRequest> resultado =
            _sut.TestValidate(requisicao with { IdUsuario = idUsuario, IdCartao = idCartao });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdUsuarioVazio_Deve_RetornarErroNoIdUsuario(AlterarVencimentoCartaoRequest requisicao)
    {
        TestValidationResult<AlterarVencimentoCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdUsuario);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdCartaoVazio_Deve_RetornarErroNoIdCartao(AlterarVencimentoCartaoRequest requisicao)
    {
        TestValidationResult<AlterarVencimentoCartaoRequest> resultado = _sut.TestValidate(requisicao with { IdCartao = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdCartao);
    }
}
