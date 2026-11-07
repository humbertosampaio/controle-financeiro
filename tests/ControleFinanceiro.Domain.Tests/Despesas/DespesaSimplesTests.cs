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
        DateOnly dataOcorrencia = mesReferencia.Inicio.AddDays(14);

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
        pagamento.ValorProjetado.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SemCategoria_Deve_RetornarSucessoSemCategoria(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        DateOnly dataOcorrencia,
        ReaisPositivo valor)
    {
        Result<DespesaSimples> result = DespesaSimples.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            dataOcorrencia,
            FormaPagamento.Pix,
            valor);

        result.Should().Succeed()
            .Which.IdCategoria.Should().BeNull();
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
        DateOnly dataOcorrencia = mesReferencia.Inicio.AddDays(14);

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

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void Prever_Quando_FormaPixOuBoleto_Deve_RetornarDespesaPendenteNoDiaDaOcorrencia(
        int valorFormaPagamento,
        IdUsuario idUsuario,
        MesReferencia mesReferencia,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        IdDespesaRecorrente idDespesaRecorrente)
    {
        FormaPagamento formaPagamento = FormaPagamento.FromValue(valorFormaPagamento);
        DateOnly dataOcorrencia = mesReferencia.Inicio.AddDays(14);

        Result<DespesaSimples> result = DespesaSimples.Prever(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            valor,
            idDespesaRecorrente);

        DespesaSimples despesa = result.Should().Succeed().Which;
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(idBeneficiario);
        despesa.Descricao.Should().Be(descricao);
        despesa.IdCategoria.Should().Be(idCategoria);
        despesa.DataOcorrencia.Should().Be(dataOcorrencia);
        despesa.FormaPagamento.Should().Be(formaPagamento);
        despesa.IdDespesaRecorrente.Should().Be(idDespesaRecorrente);
        despesa.MesReferencia.Should().Be(mesReferencia);

        Pagamento pagamento = despesa.Pagamento;
        pagamento.DataProjetada.Should().Be(dataOcorrencia);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.ValorProjetado.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Prever_Quando_FormaPagamentoCredito_Deve_RetornarFalha(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        IdDespesaRecorrente idDespesaRecorrente)
    {
        Result<DespesaSimples> result = DespesaSimples.Prever(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito,
            valor,
            idDespesaRecorrente);

        result.Should().FailWith("Cartão de Crédito não é uma forma de pagamento válida para despesa simples");
    }

    [Theory, DomainAutoData]
    public void PagamentoDoMes_Quando_MesDaDespesa_Deve_RetornarOPagamento(DespesaSimples despesa)
    {
        Maybe<Pagamento> result = despesa.PagamentoDoMes(despesa.MesReferencia);

        result.Should().HaveValue(despesa.Pagamento);
    }

    [Theory, DomainAutoData]
    public void PagamentoDoMes_Quando_OutroMes_Deve_RetornarVazio(DespesaSimples despesa)
    {
        Maybe<Pagamento> result = despesa.PagamentoDoMes(despesa.MesReferencia.Posterior());

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void ValorVigenteDoMes_Quando_Pendente_Deve_RetornarOValorProjetado(DespesaSimples despesa)
    {
        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.MesReferencia);

        result.Should().HaveValue(despesa.Pagamento.ValorProjetado);
    }

    [Theory, DomainAutoData]
    public void ValorVigenteDoMes_Quando_OutroMes_Deve_RetornarVazio(DespesaSimples despesa)
    {
        Maybe<ReaisPositivo> result = despesa.ValorVigenteDoMes(despesa.MesReferencia.Posterior());

        result.Should().HaveNoValue();
    }
}
