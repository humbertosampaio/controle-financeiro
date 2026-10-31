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
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, cartoesDoUsuario: [], banco, sufixo, diaVencimento, diaFechamento);

        Cartao cartao = result.Should().Succeed().Subject;
        cartao.IdUsuario.Should().Be(idUsuario);
        cartao.Banco.Should().Be(banco);
        cartao.Sufixo.Should().Be(sufixo);
        cartao.DiaVencimento.Should().Be(diaVencimento);
        cartao.DiaFechamento.Should().Be(diaFechamento);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SufixoRepetidoNoMesmoBanco_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, [existente], banco, sufixo, diaVencimento, diaFechamento);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroCartaoJaCadastrado>()
            .Which.Descricao.Should().Be($"O usuário já tem um cartão {banco.Name} com final {sufixo}.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesmoSufixoEmOutroBanco_Deve_CriarOCartao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, [existente], outroBanco, sufixo, diaVencimento, diaFechamento);

        result.Should().Succeed().Which.Banco.Should().Be(outroBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesmoBancoESufixoDeOutroUsuario_Deve_CriarUmCartaoNovo(
        IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao cartaoDeOutroUsuario,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        Result<Cartao, Erro> result = CartaoFactory.Criar(idUsuario, cartoesDoUsuario: [], banco, sufixo, diaVencimento, diaFechamento);

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
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        Action act = () => CartaoFactory.Criar(idUsuario, [cartaoDeOutroUsuario], banco, sufixo, diaVencimento, diaFechamento);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void ObterDosFinais_Quando_FinaisComCartao_Deve_DevolverUmCartaoPorFinalNaOrdemDosFinais(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        Cartao primeiro,
        Cartao segundo)
    {
        Result<IReadOnlyList<Cartao>, Erro> result = CartaoFactory.ObterDosFinais(
            idUsuario,
            [segundo, primeiro],
            banco,
            [primeiro.Sufixo, segundo.Sufixo, primeiro.Sufixo]);

        result.Should().Succeed().Which.Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void ObterDosFinais_Quando_FinalSemCartao_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        Cartao existente,
        SufixoCartao outroSufixo)
    {
        Result<IReadOnlyList<Cartao>, Erro> result = CartaoFactory.ObterDosFinais(idUsuario, [existente], banco, [existente.Sufixo, outroSufixo]);

        result.Should().Fail().Which.Should().BeSameAs(ErroCartoesNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void ObterDosFinais_Quando_MesmoFinalEmOutroBanco_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        Cartao existente)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        Result<IReadOnlyList<Cartao>, Erro> result = CartaoFactory.ObterDosFinais(idUsuario, [existente], outroBanco, [existente.Sufixo]);

        result.Should().Fail().Which.Should().BeSameAs(ErroCartoesNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void ObterDosFinais_Quando_ListaComCartaoDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Cartao cartaoDeOutroUsuario)
    {
        Action act = () => CartaoFactory.ObterDosFinais(
            idUsuario,
            [cartaoDeOutroUsuario],
            cartaoDeOutroUsuario.Banco,
            [cartaoDeOutroUsuario.Sufixo]);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void ObterDoFinal_Quando_CartaoDoFinalNoMesmoBanco_Deve_RetornarOCartao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao existente)
    {
        Maybe<Cartao> result = CartaoFactory.ObterDoFinal(idUsuario, [existente], banco, sufixo);

        result.Should().HaveValue(existente);
    }

    [Theory, DomainAutoData]
    public void ObterDoFinal_Quando_MesmoFinalEmOutroBanco_Deve_RetornarVazio(
        [Frozen] IdUsuario idUsuario,
        [Frozen] SufixoCartao sufixo,
        Cartao existente)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        Maybe<Cartao> result = CartaoFactory.ObterDoFinal(idUsuario, [existente], outroBanco, sufixo);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void ObterDoFinal_Quando_ListaComCartaoDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Cartao cartaoDeOutroUsuario,
        Banco banco,
        SufixoCartao sufixo)
    {
        Action act = () => CartaoFactory.ObterDoFinal(idUsuario, [cartaoDeOutroUsuario], banco, sufixo);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
