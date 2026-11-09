using AutoFixture.Xunit2;
using ControleFinanceiro.WebApi.Endpoints.Despesas.DesfazerPagamentoFatura;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Despesas.DesfazerPagamentoFatura;

/// <summary>Testes de unidade, sem API nem banco: a regra que o validator acrescenta ao domínio, o id vazio.</summary>
public sealed class DesfazerPagamentoFaturaValidatorTests
{
    private readonly DesfazerPagamentoFaturaValidator _sut = new();

    [Theory, AutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(DesfazerPagamentoFaturaRequest requisicao)
    {
        TestValidationResult<DesfazerPagamentoFaturaRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, AutoData]
    public void Validate_Quando_IdTransacaoVazio_Deve_RetornarErroNaTransacao(DesfazerPagamentoFaturaRequest requisicao)
    {
        TestValidationResult<DesfazerPagamentoFaturaRequest> resultado = _sut.TestValidate(requisicao with { IdTransacao = Guid.Empty });

        resultado.ShouldHaveValidationErrorFor(r => r.IdTransacao);
    }
}
