using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

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
    public void QuitarCom_Deve_RegistrarOsEfetivosEPreservarOPrevisto(
        ReaisPositivo valor,
        DateOnly dataProjetada,
        IdTransacao idTransacao,
        DateOnly dataEfetiva,
        ReaisPositivo valorEfetivo)
    {
        Pagamento result = Pagamento.Pendente(valor, dataProjetada).QuitarCom(idTransacao, dataEfetiva, valorEfetivo);

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
        Pagamento quitado = Pagamento.Pendente(valor, dataProjetada).QuitarCom(idTransacao, dataEfetiva, valorEfetivo);

        Pagamento result = quitado.Reabrir();

        result.Should().Be(Pagamento.Pendente(valor, dataProjetada));
    }
}
