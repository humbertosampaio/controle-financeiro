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

        FaturaCartao result = FaturaCartao.DoCartao(cartao, [cartao, outroFinal, deOutroBanco], mesVencimento);

        result.Cartoes.Should().BeEquivalentTo([cartao.Id, outroFinal.Id]);
        result.Vencimento.Should().Be(cartao.VencimentoNoMes(mesVencimento));
    }

    [Theory, DomainAutoData]
    public void DoCartao_Quando_CartaoForaDaLista_Deve_IncluirOCartao(Cartao cartao, MesReferencia mesVencimento)
    {
        FaturaCartao result = FaturaCartao.DoCartao(cartao, cartoesDoUsuario: [], mesVencimento);

        result.Cartoes.Should().Equal(cartao.Id);
    }

    [Theory, DomainAutoData]
    public void DoCartao_Deve_SerAFaturaDoCartaoDaLista(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);

        FaturaCartao result = FaturaCartao.DoCartao(cartao.Id, [outroFinal, cartao], mesVencimento);

        result.Should().Be(FaturaCartao.DoCartao(cartao, [outroFinal, cartao], mesVencimento));
    }

    [Theory, DomainAutoData]
    public void DaCompra_Deve_VencerNoMesDaFaturaEmQueACompraCai(Cartao cartao, DateOnly dataCompra)
    {
        FaturaCartao result = FaturaCartao.DaCompra(cartao.Id, dataCompra, [cartao]);

        result.Vencimento.Should().Be(cartao.VencimentoDaCompra(dataCompra));
    }

    [Theory, DomainAutoData]
    public void DoCartao_Quando_CartaoForaDaLista_Deve_LancarExcecao(Cartao cartao, MesReferencia mesVencimento)
    {
        Action act = () => FaturaCartao.DoCartao(cartao.Id, cartoesDoUsuario: [], mesVencimento);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesDoUsuario");
    }

    [Theory, DomainAutoData]
    public void DaCompra_Quando_CartaoForaDaLista_Deve_LancarExcecao(Cartao cartao, DateOnly dataCompra)
    {
        Action act = () => FaturaCartao.DaCompra(cartao.Id, dataCompra, cartoesDoUsuario: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_CartaoDeOutraFatura_Deve_SerFalso(Cartao cartao, MesReferencia mesVencimento)
    {
        Cartao deOutroBanco = cartao.EmOutroBanco();

        FaturaCartao.DoCartao(cartao, [cartao, deOutroBanco], mesVencimento).Contem(deOutroBanco.Id).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Igualdade_Quando_MesmosCartoesEmOutraOrdem_Deve_SerIgual(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);

        FaturaCartao.DoCartao(cartao, [cartao, outroFinal], mesVencimento)
            .Should().Be(FaturaCartao.DoCartao(outroFinal, [outroFinal, cartao], mesVencimento));
    }
}
