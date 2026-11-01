using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Contas;

public sealed class ContaBancariaTests
{
    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OsDoisIguais_Deve_SerVerdadeiro(ContaBancaria sut)
    {
        sut.PossuiBancoESufixo(sut.Banco, sut.Sufixo).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OutroBanco_Deve_SerFalso(ContaBancaria sut)
    {
        Banco outroBanco = Banco.List.First(b => b != sut.Banco);

        sut.PossuiBancoESufixo(outroBanco, sut.Sufixo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void PossuiBancoESufixo_Quando_OutroSufixo_Deve_SerFalso(ContaBancaria sut)
    {
        SufixoConta outroSufixo = sut.Sufixo == (SufixoConta)"0000" ? (SufixoConta)"0001" : (SufixoConta)"0000";

        sut.PossuiBancoESufixo(sut.Banco, outroSufixo).Should().BeFalse();
    }
}
