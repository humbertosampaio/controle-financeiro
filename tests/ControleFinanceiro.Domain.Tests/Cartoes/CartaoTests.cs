using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class CartaoTests
{
    [Theory, DomainAutoData]
    public void AlterarCicloFatura_Deve_TrocarOsDiasDeVencimentoEDeFechamento(
        Cartao sut,
        DiaVencimentoCartao outroVencimento,
        DiaFechamentoCartao outroFechamento)
    {
        sut.AlterarCicloFatura(outroVencimento, outroFechamento);

        sut.DiaVencimento.Should().Be(outroVencimento);
        sut.DiaFechamento.Should().Be(outroFechamento);
    }

    [Theory]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(Cartao.ToleranciaDiasVencimento)]
    public void ConferirVencimento_Quando_DoDiaCadastradoAteATolerancia_Deve_TerSucesso(int diasDepois, Cartao sut, MesReferencia mes)
    {
        DateOnly vencimento = mes.NoDia(sut.DiaVencimento).AddDays(diasDepois);

        Result result = sut.ConferirVencimento((VencimentoFatura)vencimento);

        result.Should().Succeed();
    }

    [Theory]
    [InlineDomainAutoData(-1)]
    [InlineDomainAutoData(Cartao.ToleranciaDiasVencimento + 1)]
    public void ConferirVencimento_Quando_AntesDoDiaCadastradoOuDepoisDaTolerancia_Deve_FalharComOAviso(int diasDepois, Cartao sut, MesReferencia mes)
    {
        DateOnly vencimento = mes.NoDia(sut.DiaVencimento).AddDays(diasDepois);

        Result result = sut.ConferirVencimento((VencimentoFatura)vencimento);

        result.Should().FailWith("O vencimento da fatura não fecha com o dia de vencimento cadastrado no cartão: confira a data do vencimento e o ciclo do cartão.");
    }

    [Theory, DomainAutoData]
    public void VencimentoNoMes_Deve_SerODiaDeVencimentoDoCartaoNoMes(Cartao sut, MesReferencia mesVencimento)
    {
        VencimentoFatura result = sut.VencimentoNoMes(mesVencimento);

        result.Should().Be(VencimentoFatura.Projetado(mesVencimento, sut.DiaVencimento));
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

        DateOnly result = cartao.DataOcorrenciaQueVenceEm(
            (MesReferencia)(mesVencimento, anoVencimento),
            (DiaOcorrenciaRecorrente)dia);

        result.Should().Be(new DateOnly(anoEsperado, mesEsperado, dia));
    }

    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OsDoisIguais_Deve_SerVerdadeiro(Cartao sut)
    {
        sut.PossuiBancoESufixo(sut.Banco, sut.Sufixo).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OutroBanco_Deve_SerFalso(Cartao sut)
    {
        Banco outroBanco = Banco.List.First(b => b != sut.Banco);

        sut.PossuiBancoESufixo(outroBanco, sut.Sufixo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OutroSufixo_Deve_SerFalso(Cartao sut)
    {
        SufixoCartao outroSufixo = sut.Sufixo == (SufixoCartao)"0000" ? (SufixoCartao)"0001" : (SufixoCartao)"0000";

        sut.PossuiBancoESufixo(sut.Banco, outroSufixo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaMesmaFaturaQue_Quando_OutroFinalComOMesmoBancoEOsMesmosDias_Deve_SerVerdadeiro(Cartao sut, SufixoCartao outroSufixo)
    {
        sut.EhDaMesmaFaturaQue(sut.OutroFinal(outroSufixo)).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhDaMesmaFaturaQue_Quando_OutroBanco_Deve_SerFalso(Cartao sut)
    {
        sut.EhDaMesmaFaturaQue(sut.EmOutroBanco()).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaMesmaFaturaQue_Quando_OutroVencimento_Deve_SerFalso(Cartao sut, SufixoCartao outroSufixo, DiaVencimentoCartao outroVencimento)
    {
        Cartao outro = sut.OutroFinal(outroSufixo);
        outro.AlterarCicloFatura(outroVencimento, sut.DiaFechamento);

        sut.EhDaMesmaFaturaQue(outro).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhDaMesmaFaturaQue_Quando_OutroFechamento_Deve_SerFalso(Cartao sut, SufixoCartao outroSufixo, DiaFechamentoCartao outroFechamento)
    {
        Cartao outro = sut.OutroFinal(outroSufixo);
        outro.AlterarCicloFatura(sut.DiaVencimento, outroFechamento);

        sut.EhDaMesmaFaturaQue(outro).Should().BeFalse();
    }

    private static Cartao CartaoComDias(Fixture fixture, int diaFechamento, int diaVencimento)
    {
        fixture.Inject((DiaFechamentoCartao)diaFechamento);
        fixture.Inject((DiaVencimentoCartao)diaVencimento);
        return fixture.Create<Cartao>();
    }
}
