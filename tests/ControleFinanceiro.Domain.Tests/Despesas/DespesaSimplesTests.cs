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
        DateTimeOffset dataOcorrencia = mesReferencia.Inicio.AddDays(14).ToLocalDateTimeOffset();

        Result<DespesaSimples> result = DespesaSimples.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Pix,
            valor);

        DespesaSimples despesa = result.Should().Succeed().Which;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(idBeneficiario);
        despesa.Descricao.Should().Be(descricao);
        despesa.IdCategoria.Should().Be(idCategoria);
        despesa.DataOcorrencia.Should().Be(dataOcorrencia);
        despesa.FormaPagamento.Should().Be(FormaPagamento.Pix);
        despesa.MesReferencia.Should().Be(mesReferencia);

        Pagamento pagamento = despesa.Pagamento;
        pagamento.DataProjetada.Should().Be(dataOcorrencia);
        pagamento.DataEfetiva.Should().Be(dataOcorrencia);
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
        DateTimeOffset dataOcorrencia = mesReferencia.Inicio.AddDays(14).ToLocalDateTimeOffset();

        Result<DespesaSimples> result = DespesaSimples.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito,
            valor);

        result.Should().FailWith("Cartão de Crédito não é uma forma de pagamento válida para despesa simples");
    }
}
