using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class VencimentoFaturaCartaoTests
{
    [Theory]
    // Vencimento depois do fechamento: a fatura vence no mês em que fecha.
    [InlineDomainAutoData(10, 20, 2026, 3, 5, 3, 2026)]
    [InlineDomainAutoData(10, 20, 2026, 3, 10, 3, 2026)]
    [InlineDomainAutoData(10, 20, 2026, 3, 11, 4, 2026)]
    [InlineDomainAutoData(10, 20, 2026, 3, 31, 4, 2026)]
    [InlineDomainAutoData(10, 20, 2026, 12, 31, 1, 2027)]
    // Vencimento antes do fechamento: a fatura vence no mês seguinte ao que fecha.
    [InlineDomainAutoData(25, 5, 2026, 3, 5, 4, 2026)]
    [InlineDomainAutoData(25, 5, 2026, 3, 25, 4, 2026)]
    [InlineDomainAutoData(25, 5, 2026, 3, 26, 5, 2026)]
    [InlineDomainAutoData(25, 5, 2026, 3, 31, 5, 2026)]
    [InlineDomainAutoData(25, 5, 2026, 12, 26, 2, 2027)]
    // Os dois dias iguais contam como vencimento no mês seguinte.
    [InlineDomainAutoData(15, 15, 2026, 3, 15, 4, 2026)]
    [InlineDomainAutoData(15, 15, 2026, 3, 16, 5, 2026)]
    public void MesVencimento_Deve_SeguirOFechamentoEOVencimentoDoCartao(
        int diaFechamento,
        int diaVencimento,
        int anoCompra,
        int mesCompra,
        int diaCompra,
        int mesEsperado,
        int anoEsperado,
        Fixture fixture)
    {
        Cartao cartao = CartaoComDias(fixture, diaFechamento, diaVencimento);

        MesReferencia result = VencimentoFaturaCartao.MesVencimento(cartao, new DateOnly(anoCompra, mesCompra, diaCompra));

        result.Should().Be((MesReferencia)(mesEsperado, anoEsperado));
    }

    [Theory]
    [InlineDomainAutoData(10, 20, 3, 2026, 5, 2026, 3)]
    [InlineDomainAutoData(10, 20, 3, 2026, 10, 2026, 3)]
    [InlineDomainAutoData(10, 20, 3, 2026, 11, 2026, 2)]
    [InlineDomainAutoData(10, 20, 1, 2027, 11, 2026, 12)]
    [InlineDomainAutoData(25, 5, 4, 2026, 5, 2026, 3)]
    [InlineDomainAutoData(25, 5, 4, 2026, 25, 2026, 3)]
    [InlineDomainAutoData(25, 5, 4, 2026, 26, 2026, 2)]
    [InlineDomainAutoData(25, 5, 1, 2027, 5, 2026, 12)]
    [InlineDomainAutoData(25, 5, 2, 2027, 26, 2026, 12)]
    [InlineDomainAutoData(15, 15, 4, 2026, 15, 2026, 3)]
    [InlineDomainAutoData(15, 15, 4, 2026, 16, 2026, 2)]
    public void DataOcorrenciaQueVenceEm_Deve_RecuarAteOMesDaCompra(
        int diaFechamento,
        int diaVencimento,
        int mesVencimento,
        int anoVencimento,
        int dia,
        int anoEsperado,
        int mesEsperado,
        Fixture fixture)
    {
        Cartao cartao = CartaoComDias(fixture, diaFechamento, diaVencimento);

        DateOnly result = VencimentoFaturaCartao.DataOcorrenciaQueVenceEm(
            cartao,
            (MesReferencia)(mesVencimento, anoVencimento),
            (DiaOcorrenciaRecorrente)dia);

        result.Should().Be(new DateOnly(anoEsperado, mesEsperado, dia));
    }

    [Theory, DomainAutoData]
    public void DataOcorrenciaQueVenceEm_Deve_CairNaFaturaQueVenceNoMesPedido(
        Cartao cartao,
        MesReferencia mesVencimento,
        DiaOcorrenciaRecorrente dia)
    {
        DateOnly dataOcorrencia = VencimentoFaturaCartao.DataOcorrenciaQueVenceEm(cartao, mesVencimento, dia);

        VencimentoFaturaCartao.MesVencimento(cartao, dataOcorrencia).Should().Be(mesVencimento);
    }

    private static Cartao CartaoComDias(Fixture fixture, int diaFechamento, int diaVencimento)
    {
        fixture.Inject((DiaFechamentoCartao)diaFechamento);
        fixture.Inject((DiaVencimentoCartao)diaVencimento);
        return fixture.Create<Cartao>();
    }
}
