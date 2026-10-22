using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class PagamentoTests
{
    [Theory, DomainAutoData]
    public void Pendente_Deve_CriarPagamentoPendente(ReaisPositivo valor, DateOnly dataProjetada)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada);

        pagamento.Valor.Should().Be(valor);
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.DataProjetada.Should().Be(dataProjetada.ToLocalDateTimeOffset());
        pagamento.DataEfetiva.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Pago_Deve_CriarPagamentoPago(ReaisPositivo valor, DateTimeOffset dataProjetada, DateTimeOffset dataEfetiva)
    {
        Pagamento pagamento = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        pagamento.Valor.Should().Be(valor);
        pagamento.Status.Should().Be(StatusPagamento.Pago);
        pagamento.DataProjetada.Should().Be(dataProjetada);
        pagamento.DataEfetiva.Should().Be(dataEfetiva);
    }

    [Theory, DomainAutoData]
    public void EstaPago_Quando_PagamentoPendente_Deve_RetornarFalse(ReaisPositivo valor, DateOnly dataProjetada)
    {
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada);

        pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EstaPago_Quando_PagamentoPago_Deve_RetornarTrue(ReaisPositivo valor, DateTimeOffset dataProjetada, DateTimeOffset dataEfetiva)
    {
        Pagamento pagamento = Pagamento.Pago(valor, dataProjetada, dataEfetiva);

        pagamento.EstaPago.Should().BeTrue();
    }
}
