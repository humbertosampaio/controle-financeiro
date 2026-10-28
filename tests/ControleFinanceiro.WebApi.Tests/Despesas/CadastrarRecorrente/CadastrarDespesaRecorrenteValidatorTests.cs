using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Despesas.CadastrarRecorrente;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio, o cartão no crédito e só
/// nele. Os demais campos só delegam aos value objects e aos ids, já testados no domínio; que cada regra está ligada ao
/// campo certo, o teste de integração do endpoint confere.
/// </summary>
public sealed class CadastrarDespesaRecorrenteValidatorTests
{
    private readonly CadastrarDespesaRecorrenteValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_CreditoComCartao_Deve_Passar(
        Guid idUsuario,
        Guid idCartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        CadastrarDespesaRecorrenteRequest noCredito = requisicao with
        {
            IdUsuario = idUsuario,
            FormaPagamento = FormaPagamento.Credito.Value,
            IdCartao = idCartao,
        };

        TestValidationResult<CadastrarDespesaRecorrenteRequest> resultado = _sut.TestValidate(noCredito);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineApiAutoData(1)] // Pix
    [InlineApiAutoData(3)] // Boleto
    public void Validate_Quando_PixOuBoletoSemCartao_Deve_Passar(
        int formaPagamento,
        Guid idUsuario,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        CadastrarDespesaRecorrenteRequest semCartao = requisicao with
        {
            IdUsuario = idUsuario,
            FormaPagamento = formaPagamento,
            IdCartao = null,
        };

        TestValidationResult<CadastrarDespesaRecorrenteRequest> resultado = _sut.TestValidate(semCartao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_CreditoSemCartao_Deve_RetornarErroNoIdCartao(CadastrarDespesaRecorrenteRequest requisicao)
    {
        CadastrarDespesaRecorrenteRequest semCartao = requisicao with { FormaPagamento = FormaPagamento.Credito.Value, IdCartao = null };

        TestValidationResult<CadastrarDespesaRecorrenteRequest> resultado = _sut.TestValidate(semCartao);

        resultado.ShouldHaveValidationErrorFor(r => r.IdCartao);
    }

    [Theory]
    [InlineApiAutoData(1)] // Pix
    [InlineApiAutoData(3)] // Boleto
    public void Validate_Quando_PixOuBoletoComCartao_Deve_RetornarErroNoIdCartao(
        int formaPagamento,
        Guid idCartao,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        CadastrarDespesaRecorrenteRequest comCartao = requisicao with { FormaPagamento = formaPagamento, IdCartao = idCartao };

        TestValidationResult<CadastrarDespesaRecorrenteRequest> resultado = _sut.TestValidate(comCartao);

        resultado.ShouldHaveValidationErrorFor(r => r.IdCartao);
    }

    [Theory]
    [InlineApiAutoData(0)]
    [InlineApiAutoData(4)]
    public void Validate_Quando_FormaPagamentoDesconhecida_Deve_RetornarErroNaFormaPagamento(
        int formaPagamento,
        CadastrarDespesaRecorrenteRequest requisicao)
    {
        TestValidationResult<CadastrarDespesaRecorrenteRequest> resultado = _sut.TestValidate(
            requisicao with { FormaPagamento = formaPagamento });

        resultado.ShouldHaveValidationErrorFor(r => r.FormaPagamento);
    }
}
