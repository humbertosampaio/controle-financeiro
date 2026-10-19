using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Usuarios;

public sealed class DespesaRecorrenteTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_CreditoComCartao_Deve_RetornarSucesso(
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        IdCartao idCartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        Result<DespesaRecorrente> result = DespesaRecorrente.Criar(
            idBeneficiario, descricao, idCategoria, FormaPagamento.Credito, idCartao, valor, diaCompra);

        result.Should().Succeed().Which.IdCartao.Should().Be(idCartao);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CreditoSemCartao_Deve_RetornarFalha(
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        Result<DespesaRecorrente> result = DespesaRecorrente.Criar(
            idBeneficiario, descricao, idCategoria, FormaPagamento.Credito, null, valor, diaCompra);

        result.Should().FailWith("Despesa recorrente no crédito precisa de um cartão.");
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void Criar_Quando_NaoCreditoComCartao_Deve_RetornarFalha(
        int valorFormaPagamento,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        IdCartao idCartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);

        Result<DespesaRecorrente> result = DespesaRecorrente.Criar(
            idBeneficiario, descricao, idCategoria, formaPagamento, idCartao, valor, diaCompra);

        result.Should().FailWith($"Despesa recorrente paga com {formaPagamento.Name} não pode ter cartão.");
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void Criar_Quando_NaoCreditoSemCartao_Deve_RetornarSucesso(
        int valorFormaPagamento,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);

        Result<DespesaRecorrente> result = DespesaRecorrente.Criar(
            idBeneficiario, descricao, idCategoria, formaPagamento, null, valor, diaCompra);

        result.Should().Succeed().Which.IdCartao.Should().BeNull();
    }
}
