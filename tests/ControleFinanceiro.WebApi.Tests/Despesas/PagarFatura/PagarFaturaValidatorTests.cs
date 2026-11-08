using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio, os ids vazios. O mês e o ano só
/// delegam às regras compartilhadas; que cada regra está ligada ao campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class PagarFaturaValidatorTests
{
    private readonly PagarFaturaValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(Guid idUsuario, PagarFaturaRequest requisicao)
    {
        TestValidationResult<PagarFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdTransacaoVazio_Deve_RetornarErroNaTransacao(Guid idUsuario, PagarFaturaRequest requisicao)
    {
        TestValidationResult<PagarFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario, IdTransacao = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdTransacao);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_IdCartaoVazio_Deve_RetornarErroNoCartao(Guid idUsuario, PagarFaturaRequest requisicao)
    {
        TestValidationResult<PagarFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario, IdCartao = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdCartao);
    }
}
