using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class DespesaSimplesTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ParametrosValidosEFormaPixOuBoleto_Deve_RetornarSucesso(
        IdUsuario idUsuario,
        MesReferencia mesReferencia,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor)
    {
        DateTimeOffset dataCompra = mesReferencia.Inicio.AddDays(14).ToLocalDateTimeOffset();

        Result<DespesaSimples> result = DespesaSimples.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataCompra,
            FormaPagamento.Pix,
            valor);

        DespesaSimples despesa = result.Should().Succeed().Which;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(idBeneficiario);
        despesa.Descricao.Should().Be(descricao);
        despesa.IdCategoria.Should().Be(idCategoria);
        despesa.DataCompra.Should().Be(dataCompra);
        despesa.FormaPagamento.Should().Be(FormaPagamento.Pix);
        despesa.MesReferencia.Should().Be(mesReferencia);

        Pagamento pagamento = despesa.Pagamento;
        pagamento.DataProjetada.Should().Be(dataCompra);
        pagamento.DataEfetiva.Should().Be(dataCompra);
        pagamento.Status.Should().Be(StatusPagamento.Pago);
        pagamento.Valor.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoCredito_Deve_RetornarFalha(
        IdUsuario idUsuario,
        MesReferencia mesReferencia,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor)
    {
        DateTimeOffset dataCompra = mesReferencia.Inicio.AddDays(14).ToLocalDateTimeOffset();

        Result<DespesaSimples> result = DespesaSimples.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataCompra,
            FormaPagamento.Credito,
            valor);

        result.Should().FailWith("Cartão de Crédito não é uma forma de pagamento válida para despesa simples");
    }
}
