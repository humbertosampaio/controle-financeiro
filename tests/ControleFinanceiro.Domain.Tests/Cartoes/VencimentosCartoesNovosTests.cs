using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class VencimentosCartoesNovosTests
{
    [Theory, DomainAutoData]
    public void Para_Quando_FinalInformado_Deve_UsarODiaDoFinalEmVezDoDiaUnico(
        SufixoCartao sufixo,
        DiaVencimentoCartao diaDoFinal,
        DiaVencimentoCartao diaUnico)
    {
        VencimentosCartoesNovos sut = VencimentosCartoesNovos.Criar(
            diaUnico, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [sufixo] = diaDoFinal }).Value;

        sut.Para(sufixo).Should().Be(diaDoFinal);
        sut.EhPadrao(sufixo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Para_Quando_FinalNaoInformado_Deve_UsarODiaUnico(
        SufixoCartao sufixo,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao diaDoOutroFinal,
        DiaVencimentoCartao diaUnico)
    {
        VencimentosCartoesNovos sut = VencimentosCartoesNovos.Criar(
            diaUnico, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [outroSufixo] = diaDoOutroFinal }).Value;

        sut.Para(sufixo).Should().Be(diaUnico);
        sut.EhPadrao(sufixo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Para_Quando_SemDiaUnicoNemDiaDoFinal_Deve_UsarODiaPadrao(
        SufixoCartao sufixo,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao diaDoOutroFinal)
    {
        VencimentosCartoesNovos sut = VencimentosCartoesNovos.Criar(
            diaUnico: null, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [outroSufixo] = diaDoOutroFinal }).Value;

        sut.Para(sufixo).Should().Be(DiaVencimentoCartao.Padrao);
        sut.EhPadrao(sufixo).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FinalRepetido_Deve_RetornarFalha(
        SufixoCartao sufixo,
        DiaVencimentoCartao dia,
        DiaVencimentoCartao outroDia,
        DiaVencimentoCartao diaUnico)
    {
        Result<VencimentosCartoesNovos> result = VencimentosCartoesNovos.Criar(diaUnico, [new(sufixo, dia), new(sufixo, outroDia)]);

        result.Should().FailWith("Os vencimentos não podem repetir o final do cartão.");
    }

    [Fact]
    public void Padrao_Deve_SerODia1()
    {
        DiaVencimentoCartao.Padrao.Value.Should().Be(1);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosFinaisEmOutraOrdem_Deve_SerIgual(
        SufixoCartao sufixo,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao dia,
        DiaVencimentoCartao outroDia,
        DiaVencimentoCartao diaUnico)
    {
        VencimentosCartoesNovos vencimentos = VencimentosCartoesNovos.Criar(
            diaUnico, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [sufixo] = dia, [outroSufixo] = outroDia }).Value;

        VencimentosCartoesNovos outros = VencimentosCartoesNovos.Criar(
            diaUnico, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [outroSufixo] = outroDia, [sufixo] = dia }).Value;

        outros.Should().Be(vencimentos);
    }
}
