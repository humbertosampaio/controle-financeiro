using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class FaturaCartaoTests
{
    [Theory, DomainAutoData]
    public void Do_Deve_JuntarOCartaoEOsFinaisDaMesmaFatura(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        Cartao deOutroBanco = cartao.EmOutroBanco();

        FaturaCartao result = FaturaCartao.Do(cartao, [cartao, outroFinal, deOutroBanco], mesVencimento);

        result.Cartoes.Should().BeEquivalentTo([cartao.Id, outroFinal.Id]);
        result.MesVencimento.Should().Be(mesVencimento);
    }

    [Theory, DomainAutoData]
    public void Do_Quando_CartaoForaDaLista_Deve_IncluirOCartao(Cartao cartao, MesReferencia mesVencimento)
    {
        FaturaCartao result = FaturaCartao.Do(cartao, cartoesDoUsuario: [], mesVencimento);

        result.Cartoes.Should().Equal(cartao.Id);
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_CartaoDeOutraFatura_Deve_SerFalso(Cartao cartao, MesReferencia mesVencimento)
    {
        Cartao deOutroBanco = cartao.EmOutroBanco();

        FaturaCartao.Do(cartao, [cartao, deOutroBanco], mesVencimento).Contem(deOutroBanco.Id).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Igualdade_Quando_MesmosCartoesEmOutraOrdem_Deve_SerIgual(Cartao cartao, SufixoCartao outroSufixo, MesReferencia mesVencimento)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);

        FaturaCartao.Do(cartao, [cartao, outroFinal], mesVencimento)
            .Should().Be(FaturaCartao.Do(outroFinal, [outroFinal, cartao], mesVencimento));
    }
}
