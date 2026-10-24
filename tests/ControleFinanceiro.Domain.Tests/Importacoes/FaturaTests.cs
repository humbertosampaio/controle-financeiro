using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class FaturaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_SemLancamentos_Deve_RetornarFalha(Banco banco)
    {
        Result<Fatura> result = Fatura.Criar(banco, lancamentos: []);

        result.Should().FailWith("A fatura não traz lançamentos.");
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarOBancoEOsLancamentosNaOrdemDoArquivo(Banco banco, LancamentoFatura primeiro, LancamentoFatura segundo)
    {
        Result<Fatura> result = Fatura.Criar(banco, [primeiro, segundo]);

        Fatura fatura = result.Should().Succeed().Subject;
        fatura.Banco.Should().Be(banco);
        fatura.Lancamentos.Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void Sufixos_Deve_TrazerOsFinaisDistintosNaOrdemDaPrimeiraAparicao(
        Banco banco,
        Lancamento lancamento,
        SufixoCartao fisico,
        SufixoCartao virtualCartao)
    {
        Fatura sut = Fatura.Criar(
                banco,
                [
                    LancamentoFatura.Criar(lancamento, virtualCartao),
                    LancamentoFatura.Criar(lancamento, fisico),
                    LancamentoFatura.Criar(lancamento, virtualCartao),
                ])
            .Value;

        sut.Sufixos.Should().Equal(virtualCartao, fisico);
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Deve_TrazerSoOsDoFinalNaOrdemDoArquivo(
        Banco banco,
        Lancamento primeiro,
        Lancamento doOutroCartao,
        Lancamento segundo,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
                banco,
                [
                    LancamentoFatura.Criar(primeiro, sufixo),
                    LancamentoFatura.Criar(doOutroCartao, outroSufixo),
                    LancamentoFatura.Criar(segundo, sufixo),
                ])
            .Value;

        sut.LancamentosCartao(sufixo).Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_DatasForaDeOrdem_Deve_IrDaMenorAMaiorDataDeCompraDoCartao(
        Banco banco,
        Lancamento lancamento,
        DateOnly dia,
        SufixoCartao sufixo,
        SufixoCartao outroSufixo)
    {
        Fatura sut = Fatura.Criar(
                banco,
                [
                    LancamentoFatura.Criar(NaData(lancamento, dia), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-270)), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-400)), outroSufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(-10)), sufixo),
                    LancamentoFatura.Criar(NaData(lancamento, dia.AddDays(5)), outroSufixo),
                ])
            .Value;

        sut.PeriodoCartao(sufixo).Should().Be((PeriodoArquivo)(dia.AddDays(-270), dia));
    }

    [Theory, DomainAutoData]
    public void LancamentosCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<IReadOnlyList<Lancamento>> act = () => sut.LancamentosCartao(foraDaFatura);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("sufixo");
    }

    [Theory, DomainAutoData]
    public void PeriodoCartao_Quando_FinalForaDaFatura_Deve_LancarExcecao(Fatura sut, SufixoCartao foraDaFatura)
    {
        Func<PeriodoArquivo> act = () => sut.PeriodoCartao(foraDaFatura);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("sufixo");
    }

    private static Lancamento NaData(Lancamento lancamento, DateOnly data)
    {
        return Lancamento.Criar(
            data,
            lancamento.Hora,
            lancamento.Valor,
            lancamento.Descricao,
            lancamento.Contraparte,
            lancamento.Parcela,
            lancamento.CategoriaBanco,
            lancamento.Identidade);
    }
}
