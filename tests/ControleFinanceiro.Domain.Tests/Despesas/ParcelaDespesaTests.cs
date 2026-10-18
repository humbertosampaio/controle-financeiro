using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class ParcelaDespesaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ParametrosValidos_Deve_RetornarSucesso(
        MesReferencia mesReferencia,
        ReaisPositivo valor,
        int numeroParcela,
        DiaVencimentoCartao diaVencimento)
    {
        Result<ParcelaDespesa> result = ParcelaDespesa.Criar(numeroParcela, mesReferencia, diaVencimento, valor);

        ParcelaDespesa parcela = result.Should().Succeed().Which;
        parcela.NumeroParcela.Should().Be(numeroParcela);
        parcela.MesReferencia.Should().Be(mesReferencia);

        Pagamento pagamento = parcela.Pagamento;
        pagamento.Should().NotBeNull();
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.Valor.Should().Be(valor);
        pagamento.DataProjetada.Should().Be(mesReferencia.NoDia(diaVencimento).ToLocalDateTimeOffset());
        pagamento.DataEfetiva.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_NumeroParcelaZero_Deve_RetornarFalha(MesReferencia mesReferencia, ReaisPositivo valor, DiaVencimentoCartao diaVencimento)
    {
        Result<ParcelaDespesa> result = ParcelaDespesa.Criar(
            numeroParcela: 0,
            mesReferencia,
            diaVencimento,
            valor);

        result.Should().FailWith("Número da parcela deve ser maior que zero.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_NumeroParcelaNegativo_Deve_RetornarFalha(
        int numeroParcela,
        MesReferencia mesReferencia,
        ReaisPositivo valor,
        DiaVencimentoCartao diaVencimento)
    {
        Result<ParcelaDespesa> result = ParcelaDespesa.Criar(
            -numeroParcela,
            mesReferencia,
            diaVencimento,
            valor);

        result.Should().FailWith("Número da parcela deve ser maior que zero.");
    }
}
