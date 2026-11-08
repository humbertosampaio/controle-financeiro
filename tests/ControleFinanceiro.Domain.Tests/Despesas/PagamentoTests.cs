using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class PagamentoTests
{
    [Theory, DomainAutoData]
    public void Pendente_Deve_CriarPagamentoPendente(ReaisPositivo valor, DateOnly dataProjetada)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada);

        pagamento.ValorProjetado.Should().Be(valor);
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.DataProjetada.Should().Be(dataProjetada);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.ValorEfetivo.Should().BeNull();
        pagamento.IdTransacao.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void ValorVigenteCom_Quando_PendenteSemCobranca_Deve_RetornarOValorProjetado(ReaisPositivo valor, DateOnly dataProjetada)
    {
        ReaisPositivo result = Pagamento.Pendente(valor, dataProjetada).ValorVigenteCom(cobranca: null);

        result.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void ValorVigenteCom_Quando_PendenteCobrado_Deve_RetornarOValorCobrado(ReaisPositivo valor, DateOnly dataProjetada, Cobranca cobranca)
    {
        ReaisPositivo result = Pagamento.Pendente(valor, dataProjetada).ValorVigenteCom(cobranca);

        result.Should().Be(cobranca.Valor);
    }

    [Theory, DomainAutoData]
    public void ValorVigenteCom_Quando_PagoECobrado_Deve_RetornarOValorEfetivo(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        DateOnly dataEfetiva,
        Cobranca cobranca)
    {
        Pagamento pago = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        ReaisPositivo result = pago.ValorVigenteCom(cobranca);

        result.Should().Be(pago.ValorEfetivo);
    }

    [Theory, DomainAutoData]
    public void PagarPelaFatura_Quando_PendenteCobrado_Deve_PagarComADataDaSaidaEOValorCobrado(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        Cobranca cobranca,
        IdTransacao idSaida,
        DateOnly dataPagamento)
    {
        Pagamento pendente = Pagamento.Pendente(valor, dataProjetada);

        Maybe<Pagamento> result = pendente.PagarPelaFatura(cobranca, idSaida, dataPagamento);

        result.Should().HaveValue(pendente.PagarCom(idSaida, dataPagamento, cobranca.Valor));
    }

    [Theory, DomainAutoData]
    public void PagarPelaFatura_Quando_PendenteSemCobranca_Deve_RetornarNada(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idSaida,
        DateOnly dataPagamento)
    {
        Maybe<Pagamento> result = Pagamento.Pendente(valor, dataProjetada).PagarPelaFatura(cobranca: null, idSaida, dataPagamento);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void PagarPelaFatura_Quando_JaPago_Deve_RetornarNada(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        DateOnly dataEfetiva,
        Cobranca cobranca,
        IdTransacao idSaida,
        DateOnly dataPagamento)
    {
        Maybe<Pagamento> result = Pagamento.Pago(valor, dataProjetada, dataEfetiva).PagarPelaFatura(cobranca, idSaida, dataPagamento);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void Pago_Deve_CriarPagamentoPago(ReaisPositivo valor, DateOnly dataProjetada, DateOnly dataEfetiva)
    {
        Pagamento pagamento = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        pagamento.ValorProjetado.Should().Be(valor);
        pagamento.Status.Should().Be(StatusPagamento.Pago);
        pagamento.DataProjetada.Should().Be(dataProjetada);
        pagamento.DataEfetiva.Should().Be(dataEfetiva);
        pagamento.ValorEfetivo.Should().Be(valor);
        pagamento.IdTransacao.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Pendente_Deve_GuardarOMesDaDataProjetada(ReaisPositivo valor, DateOnly dataProjetada)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada);

        pagamento.MesReferencia.Should().Be((MesReferencia)dataProjetada);
    }

    [Theory, DomainAutoData]
    public void Pago_Deve_GuardarOMesDaDataProjetada(ReaisPositivo valor, DateOnly dataProjetada, DateOnly dataEfetiva)
    {
        Pagamento pagamento = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        pagamento.MesReferencia.Should().Be((MesReferencia)dataProjetada);
    }

    [Theory, DomainAutoData]
    public void EstaPago_Quando_PagamentoPendente_Deve_RetornarFalse(ReaisPositivo valor, DateOnly dataProjetada)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada);

        pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EstaPago_Quando_PagamentoPago_Deve_RetornarTrue(ReaisPositivo valor, DateOnly dataProjetada, DateOnly dataEfetiva)
    {
        Pagamento pagamento = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        pagamento.EstaPago.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PagarCom_Deve_RegistrarOsEfetivosEPreservarOPrevisto(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idTransacao,
        DateOnly dataEfetiva,
        ReaisPositivo valorEfetivo)
    {
        Pagamento result = Pagamento.Pendente(valor, dataProjetada).PagarCom(idTransacao, dataEfetiva, valorEfetivo);

        result.Status.Should().Be(StatusPagamento.Pago);
        result.ValorProjetado.Should().Be(valor);
        result.DataProjetada.Should().Be(dataProjetada);
        result.DataEfetiva.Should().Be(dataEfetiva);
        result.ValorEfetivo.Should().Be(valorEfetivo);
        result.IdTransacao.Should().Be(idTransacao);
    }

    [Theory, DomainAutoData]
    public void Reabrir_Deve_VoltarParaPendenteSemOsEfetivos(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idTransacao,
        DateOnly dataEfetiva,
        ReaisPositivo valorEfetivo)
    {
        Pagamento pago = Pagamento.Pendente(valor, dataProjetada).PagarCom(idTransacao, dataEfetiva, valorEfetivo);

        Pagamento result = pago.Reabrir();

        result.Should().Be(Pagamento.Pendente(valor, dataProjetada));
    }

    [Theory, DomainAutoData]
    public void PagarCom_Deve_PreservarOMes(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idTransacao,
        DateOnly dataEfetiva,
        ReaisPositivo valorEfetivo)
    {
        Pagamento result = Pagamento.Pendente(valor, dataProjetada).PagarCom(idTransacao, dataEfetiva, valorEfetivo);

        result.MesReferencia.Should().Be((MesReferencia)dataProjetada);
    }

    [Theory, DomainAutoData]
    public void Reabrir_Deve_PreservarOMes(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idTransacao,
        DateOnly dataEfetiva,
        ReaisPositivo valorEfetivo)
    {
        Pagamento pago = Pagamento.Pendente(valor, dataProjetada).PagarCom(idTransacao, dataEfetiva, valorEfetivo);

        Pagamento result = pago.Reabrir();

        result.MesReferencia.Should().Be((MesReferencia)dataProjetada);
    }
}
