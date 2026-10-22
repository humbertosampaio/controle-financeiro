using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Usuarios;

public sealed class UsuarioTests
{
    [Theory, DomainAutoData]
    public void Criar_Deve_IniciarSemContas(TextoObrigatorio nome, ReaisPositivo rendaBruta)
    {
        Usuario sut = Usuario.Criar(nome, rendaBruta, []);

        sut.Contas.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void RendaLiquida_Quando_SemDescontos_Deve_RetornarRendaBruta(TextoObrigatorio nome, ReaisPositivo rendaBruta)
    {
        Usuario sut = Usuario.Criar(nome, rendaBruta, []);

        decimal result = sut.RendaLiquida;

        result.Should().Be(rendaBruta);
    }

    [Fact]
    public void RendaLiquida_Quando_ComDescontos_Deve_RetornarRendaBrutaMenosSomaDosValores()
    {
        ReaisPositivo rendaBruta = (ReaisPositivo)1000m;
        DescontoSalarial desconto1 = DescontoSalarial.Criar((TextoObrigatorio)"INSS", (ReaisPositivo)100m);
        DescontoSalarial desconto2 = DescontoSalarial.Criar((TextoObrigatorio)"IR", (ReaisPositivo)50m);
        Usuario sut = Usuario.Criar((TextoObrigatorio)"Nome", rendaBruta, [desconto1, desconto2]);

        decimal result = sut.RendaLiquida;

        result.Should().Be(850m);
    }

    [Theory, DomainAutoData]
    public void AdicionarConta_Quando_PrimeiraConta_Deve_AdicionarNaLista(Usuario sut, ContaBancaria conta)
    {
        Result result = sut.AdicionarConta(conta);

        result.Should().Succeed();
        sut.Contas.Should().ContainSingle().Which.Should().Be(conta);
    }

    [Theory, DomainAutoData]
    public void AdicionarConta_Quando_SufixoRepetidoNoMesmoBanco_Deve_RetornarFalha(
        Usuario sut,
        [Frozen] Banco banco,
        [Frozen] SufixoConta sufixo,
        ContaBancaria conta,
        ContaBancaria outraContaComOMesmoSufixo)
    {
        sut.AdicionarConta(conta).Should().Succeed();

        Result result = sut.AdicionarConta(outraContaComOMesmoSufixo);

        result.Should().FailWith($"O usuário já tem uma conta {banco.Name} com final {sufixo}.");
        sut.Contas.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void AdicionarConta_Quando_MesmoSufixoEmOutroBanco_Deve_Adicionar(
        Usuario sut,
        SufixoConta sufixo,
        TipoConta tipo)
    {
        sut.AdicionarConta(ContaBancaria.Criar(sut.Id, Banco.C6, tipo, sufixo)).Should().Succeed();

        Result result = sut.AdicionarConta(ContaBancaria.Criar(sut.Id, Banco.MercadoPago, tipo, sufixo));

        result.Should().Succeed();
        sut.Contas.Should().HaveCount(2);
    }

    [Theory, DomainAutoData]
    public void ObterOuAdicionarConta_Quando_SemContaNoBancoComOSufixo_Deve_AdicionarComOApelidoPadrao(
        Usuario sut,
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo)
    {
        Result<ContaBancaria> result = sut.ObterOuAdicionarConta(banco, tipo, sufixo);

        ContaBancaria conta = result.Should().Succeed().Subject;
        conta.Banco.Should().Be(banco);
        conta.Tipo.Should().Be(tipo);
        conta.Sufixo.Should().Be(sufixo);
        conta.Apelido.Value.Should().Be($"{tipo.Name} {banco.Name} final {sufixo}");
        sut.Contas.Should().ContainSingle().Which.Should().BeSameAs(conta);
    }

    [Theory, DomainAutoData]
    public void ObterOuAdicionarConta_Quando_ContaComMesmoBancoSufixoETipo_Deve_RetornarAContaExistente(
        Usuario sut,
        [Frozen] Banco banco,
        [Frozen] TipoConta tipo,
        [Frozen] SufixoConta sufixo,
        ContaBancaria existente)
    {
        sut.AdicionarConta(existente).Should().Succeed();

        Result<ContaBancaria> result = sut.ObterOuAdicionarConta(banco, tipo, sufixo);

        result.Should().Succeed().Which.Should().BeSameAs(existente);
        sut.Contas.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void ObterOuAdicionarConta_Quando_MesmoSufixoEmOutroBanco_Deve_AdicionarUmaContaNova(
        Usuario sut,
        TipoConta tipo,
        SufixoConta sufixo)
    {
        sut.AdicionarConta(ContaBancaria.Criar(sut.Id, Banco.MercadoPago, tipo, sufixo)).Should().Succeed();

        Result<ContaBancaria> result = sut.ObterOuAdicionarConta(Banco.C6, tipo, sufixo);

        result.Should().Succeed().Which.Banco.Should().Be(Banco.C6);
        sut.Contas.Should().HaveCount(2);
    }

    [Theory, DomainAutoData]
    public void ObterOuAdicionarConta_Quando_ContaComMesmoBancoESufixoDeOutroTipo_Deve_RetornarFalha(
        Usuario sut,
        Banco banco,
        SufixoConta sufixo)
    {
        sut.AdicionarConta(ContaBancaria.Criar(sut.Id, banco, TipoConta.ContaPagamento, sufixo)).Should().Succeed();

        Result<ContaBancaria> result = sut.ObterOuAdicionarConta(banco, TipoConta.ContaCorrente, sufixo);

        result.Should().FailWith(
            $"O usuário já tem uma conta {banco.Name} com final {sufixo}, mas do tipo {TipoConta.ContaPagamento.Name}.");
        sut.Contas.Should().ContainSingle();
    }
}
