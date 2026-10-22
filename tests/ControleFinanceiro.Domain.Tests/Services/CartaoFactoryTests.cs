using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class CartaoFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_PrimeiroCartao_Deve_CriarOCartaoDoUsuario(
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, cartoesDoUsuario: [], banco, sufixo, diaVencimento);

        Cartao cartao = result.Should().Succeed().Subject;
        cartao.IdUsuario.Should().Be(idUsuario);
        cartao.Banco.Should().Be(banco);
        cartao.Sufixo.Should().Be(sufixo);
        cartao.DiaVencimento.Should().Be(diaVencimento);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SufixoRepetidoNoMesmoBanco_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        DiaVencimentoCartao diaVencimento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, [existente], banco, sufixo, diaVencimento);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroCartaoJaCadastrado>()
            .Which.Descricao.Should().Be($"O usuário já tem um cartão {banco.Name} com final {sufixo}.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesmoSufixoEmOutroBanco_Deve_CriarOCartao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        DiaVencimentoCartao diaVencimento)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, [existente], outroBanco, sufixo, diaVencimento);

        result.Should().Succeed().Which.Banco.Should().Be(outroBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesmoBancoESufixoDeOutroUsuario_Deve_CriarUmCartaoNovo(
        IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao cartaoDeOutroUsuario,
        DiaVencimentoCartao diaVencimento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, cartoesDoUsuario: [], banco, sufixo, diaVencimento);

        Cartao cartao = result.Should().Succeed().Subject;
        cartao.Id.Should().NotBe(cartaoDeOutroUsuario.Id);
        cartao.IdUsuario.Should().Be(idUsuario);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ListaComCartaoDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Cartao cartaoDeOutroUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento)
    {
        Action act = () => CartaoFactory.Criar(idUsuario, [cartaoDeOutroUsuario], banco, sufixo, diaVencimento);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
