using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Usuarios;

public sealed class ContaBancariaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ComApelido_Deve_UsarOApelidoInformado(
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo,
        TextoObrigatorio apelido)
    {
        ContaBancaria result = ContaBancaria.Criar(banco, tipo, sufixo, apelido);

        result.Apelido.Should().Be(apelido);
    }

    [Fact]
    public void Criar_Quando_SemApelido_Deve_NomearPeloTipoBancoESufixo()
    {
        ContaBancaria result = ContaBancaria.Criar(Banco.C6, TipoConta.ContaCorrente, (SufixoConta)"123X");

        result.Apelido.Value.Should().Be("Conta corrente C6 Bank final 123X");
    }
}
