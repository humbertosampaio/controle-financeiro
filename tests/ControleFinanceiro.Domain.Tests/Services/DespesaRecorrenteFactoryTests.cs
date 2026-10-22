using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class DespesaRecorrenteFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_CreditoComCartaoDoUsuario_Deve_GuardarOUsuarioEOIdDoCartao(
        [Frozen] IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        Cartao cartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        Result<DespesaRecorrente> result = DespesaRecorrenteFactory.Criar(
            idUsuario, idBeneficiario, descricao, idCategoria, FormaPagamento.Credito, cartao, valor, diaCompra);

        DespesaRecorrente despesa = result.Should().Succeed().Subject;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdCartao.Should().Be(cartao.Id);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CreditoSemCartao_Deve_RetornarFalha(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        Result<DespesaRecorrente> result = DespesaRecorrenteFactory.Criar(
            idUsuario, idBeneficiario, descricao, idCategoria, FormaPagamento.Credito, cartao: null, valor, diaCompra);

        result.Should().FailWith("Despesa recorrente no crédito precisa de um cartão.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CreditoComCartaoDeOutroUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        Cartao cartaoDeOutroUsuario,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        Result<DespesaRecorrente> result = DespesaRecorrenteFactory.Criar(
            idUsuario, idBeneficiario, descricao, idCategoria, FormaPagamento.Credito, cartaoDeOutroUsuario, valor, diaCompra);

        result.Should().FailWith("O cartão da despesa recorrente não pertence ao usuário.");
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void Criar_Quando_NaoCreditoComCartao_Deve_RetornarFalha(
        int valorFormaPagamento,
        [Frozen] IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        Cartao cartao,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);

        Result<DespesaRecorrente> result = DespesaRecorrenteFactory.Criar(
            idUsuario, idBeneficiario, descricao, idCategoria, formaPagamento, cartao, valor, diaCompra);

        result.Should().FailWith($"Despesa recorrente paga com {formaPagamento.Name} não pode ter cartão.");
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void Criar_Quando_NaoCreditoSemCartao_Deve_GuardarOUsuarioSemCartao(
        int valorFormaPagamento,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        DiaCompraRecorrente diaCompra)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);

        Result<DespesaRecorrente> result = DespesaRecorrenteFactory.Criar(
            idUsuario, idBeneficiario, descricao, idCategoria, formaPagamento, cartao: null, valor, diaCompra);

        DespesaRecorrente despesa = result.Should().Succeed().Subject;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdCartao.Should().BeNull();
    }
}
