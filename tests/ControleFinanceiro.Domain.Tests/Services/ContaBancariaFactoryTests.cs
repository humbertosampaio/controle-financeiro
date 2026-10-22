using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class ContaBancariaFactoryTests
{
    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_SemContaNoBancoComOSufixo_Deve_CriarUmaContaNovaDoUsuario(
        IdUsuario idUsuario,
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo)
    {
        Result<ContaBancaria, Erro> result = ContaBancariaFactory.ObterOuCriar(idUsuario, contasDoUsuario: [], banco, tipo, sufixo);

        ContaBancaria conta = result.Should().Succeed().Subject;
        conta.IdUsuario.Should().Be(idUsuario);
        conta.Banco.Should().Be(banco);
        conta.Tipo.Should().Be(tipo);
        conta.Sufixo.Should().Be(sufixo);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_ContaNova_Deve_NomearPeloTipoBancoESufixo(IdUsuario idUsuario)
    {
        Result<ContaBancaria, Erro> result = ContaBancariaFactory.ObterOuCriar(
            idUsuario, contasDoUsuario: [], Banco.C6, TipoConta.ContaCorrente, (SufixoConta)"123X");

        result.Should().Succeed().Which.Apelido.Value.Should().Be("Conta corrente C6 Bank final 123X");
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_ContaComMesmoBancoSufixoETipo_Deve_RetornarAContaExistente(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] TipoConta tipo,
        [Frozen] SufixoConta sufixo,
        ContaBancaria existente)
    {
        Result<ContaBancaria, Erro> result = ContaBancariaFactory.ObterOuCriar(
            idUsuario, [existente], banco, tipo, sufixo);

        result.Should().Succeed().Which.Should().BeSameAs(existente);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_MesmoSufixoEmOutroBanco_Deve_CriarUmaContaNova(
        [Frozen] IdUsuario idUsuario,
        [Frozen] TipoConta tipo,
        [Frozen] SufixoConta sufixo,
        ContaBancaria existente)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        Result<ContaBancaria, Erro> result = ContaBancariaFactory.ObterOuCriar(
            idUsuario, [existente], outroBanco, tipo, sufixo);

        ContaBancaria conta = result.Should().Succeed().Subject;
        conta.Should().NotBe(existente);
        conta.Banco.Should().Be(outroBanco);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_ContaComMesmoBancoESufixoDeOutroTipo_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoConta sufixo,
        ContaBancaria existente)
    {
        TipoConta outroTipo = TipoConta.List.First(t => t != existente.Tipo);

        Result<ContaBancaria, Erro> result = ContaBancariaFactory.ObterOuCriar(
            idUsuario, [existente], banco, outroTipo, sufixo);

        ErroContaDeOutroTipo erro = result.Should().Fail().Which.Should().BeOfType<ErroContaDeOutroTipo>().Subject;
        erro.TipoExistente.Should().Be(existente.Tipo);
        erro.Descricao.Should().Be(
            $"O usuário já tem uma conta {banco.Name} com final {sufixo}, mas do tipo {existente.Tipo.Name}.");
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_ListaComContaDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        ContaBancaria contaDeOutroUsuario,
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo)
    {
        Action act = () => ContaBancariaFactory.ObterOuCriar(idUsuario, [contaDeOutroUsuario], banco, tipo, sufixo);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
