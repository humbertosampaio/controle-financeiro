using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class FaturaCartaoTests
{
    [Theory, DomainAutoData]
    public void DoCartao_Deve_JuntarOCartaoEOsFinaisDaMesmaFatura(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        Cartao deOutroBanco = cartao.EmOutroBanco();

        FaturaCartao result = FaturaCartao.DoCartao(cartao.Id, [cartao, outroFinal, deOutroBanco], mesVencimento);

        result.Cartoes.Should().BeEquivalentTo([cartao.Id, outroFinal.Id]);
        result.Vencimento.Should().Be(cartao.VencimentoNoMes(mesVencimento));
    }

    [Theory, DomainAutoData]
    public void DoCartao_Quando_CartaoForaDaLista_Deve_LancarExcecao(Cartao cartao, MesReferencia mesVencimento)
    {
        Action act = () => FaturaCartao.DoCartao(cartao.Id, cartoesUsuario: [], mesVencimento);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesUsuario");
    }

    [Theory, DomainAutoData]
    public void Importada_Deve_JuntarOsCartoesDoArquivoEOsDaMesmaFatura(
        Cartao cartao,
        SufixoCartao outroSufixo,
        VencimentoFatura vencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();

        FaturaCartao result = FaturaCartao.Importada(cartao.Banco, [cartao], [outroFinal, cartaoOutroBanco], vencimento);

        result.Cartoes.Should().BeEquivalentTo([cartao.Id, outroFinal.Id]);
        result.Vencimento.Should().Be(vencimento);
    }

    [Theory, DomainAutoData]
    public void Importada_Quando_SemCartoesArquivo_Deve_FicarSemCartoes(Cartao cartao, VencimentoFatura vencimento)
    {
        FaturaCartao result = FaturaCartao.Importada(cartao.Banco, cartoesArquivo: [], [cartao], vencimento);

        result.Cartoes.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Importada_Quando_CartaoArquivoDeOutroBanco_Deve_LancarExcecao(Cartao cartao, VencimentoFatura vencimento)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();

        Action act = () => FaturaCartao.Importada(cartao.Banco, [cartao, cartaoOutroBanco], cartoesUsuario: [], vencimento);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesArquivo");
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_CartaoDeOutraFatura_Deve_SerFalso(Cartao cartao, MesReferencia mesVencimento)
    {
        Cartao deOutroBanco = cartao.EmOutroBanco();

        FaturaCartao.DoCartao(cartao.Id, [cartao, deOutroBanco], mesVencimento).Contem(deOutroBanco.Id).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Igualdade_Quando_MesmosCartoesEmOutraOrdem_Deve_SerIgual(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);

        FaturaCartao.DoCartao(cartao.Id, [cartao, outroFinal], mesVencimento)
            .Should().Be(FaturaCartao.DoCartao(outroFinal.Id, [outroFinal, cartao], mesVencimento));
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_TerOBancoDoCartao(Cartao cartao, MesReferencia mesVencimento)
    {
        FaturaCartao.DoCartao(cartao.Id, [cartao], mesVencimento).Banco.Should().Be(cartao.Banco);
    }

    [Theory, DomainAutoData]
    public void Importada_Quando_SemCartoesArquivo_Deve_TerOBancoInformado(Banco banco, Cartao cartao, VencimentoFatura vencimento)
    {
        FaturaCartao.Importada(banco, cartoesArquivo: [], [cartao], vencimento).Banco.Should().Be(banco);
    }

    [Theory, DomainAutoData]
    public void Igualdade_Quando_OutroBanco_Deve_SerDiferente(Cartao cartao, VencimentoFatura vencimento)
    {
        Banco outroBanco = Banco.List.First(b => b != cartao.Banco);

        FaturaCartao.Importada(cartao.Banco, cartoesArquivo: [], [cartao], vencimento)
            .Should().NotBe(FaturaCartao.Importada(outroBanco, cartoesArquivo: [], [cartao], vencimento));
    }

    [Theory, DomainAutoData]
    public void TodasVencendoEm_Deve_JuntarOsFinaisDaMesmaFaturaESepararOsBancos(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        Cartao[] cartoes = [cartao, outroFinal, cartaoOutroBanco];

        IReadOnlyList<FaturaCartao> result = FaturaCartao.TodasVencendoEm(cartoes, mesVencimento);

        result.Should().BeEquivalentTo([
            FaturaCartao.DoCartao(cartao.Id, cartoes, mesVencimento),
            FaturaCartao.DoCartao(cartaoOutroBanco.Id, cartoes, mesVencimento)]);
    }
}
