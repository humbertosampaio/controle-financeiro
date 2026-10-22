using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Contas;

public sealed class ContaBancariaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ComApelido_Deve_UsarOApelidoInformado(
        IdUsuario idUsuario,
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo,
        TextoObrigatorio apelido)
    {
        ContaBancaria result = ContaBancaria.Criar(idUsuario, banco, tipo, sufixo, apelido);

        result.Apelido.Should().Be(apelido);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SemApelido_Deve_NomearPeloTipoBancoESufixo(IdUsuario idUsuario)
    {
        ContaBancaria result = ContaBancaria.Criar(idUsuario, Banco.C6, TipoConta.ContaCorrente, (SufixoConta)"123X");

        result.Apelido.Value.Should().Be("Conta corrente C6 Bank final 123X");
    }
}
