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

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_CartaoDoFinalNoMesmoBanco_Deve_RetornarOCartaoExistenteSemMudarOVencimento(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Banco banco,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        DiaVencimentoCartao outroDia)
    {
        DiaVencimentoCartao vencimentoOriginal = existente.DiaVencimento;
        VencimentosCartoesNovos vencimentos = VencimentosCartoesNovos.Criar(
            outroDia, new Dictionary<SufixoCartao, DiaVencimentoCartao> { [sufixo] = outroDia }).Value;

        IReadOnlyList<Cartao> result = CartaoFactory.ObterOuCriar(idUsuario, [existente], banco, [sufixo], vencimentos);

        result.Should().ContainSingle().Which.Should().BeSameAs(existente);
        existente.DiaVencimento.Should().Be(vencimentoOriginal);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_FinalSemCartao_Deve_CriarOCartaoComOVencimentoDoFinal(
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        [Frozen] DiaVencimentoCartao diaVencimento,
        VencimentosCartoesNovos vencimentos)
    {
        IReadOnlyList<Cartao> result = CartaoFactory.ObterOuCriar(idUsuario, cartoesDoUsuario: [], banco, [sufixo], vencimentos);

        Cartao cartao = result.Should().ContainSingle().Subject;
        cartao.IdUsuario.Should().Be(idUsuario);
        cartao.Banco.Should().Be(banco);
        cartao.Sufixo.Should().Be(sufixo);
        cartao.DiaVencimento.Should().Be(diaVencimento);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_MesmoFinalEmOutroBanco_Deve_CriarUmCartaoNovo(
        [Frozen] IdUsuario idUsuario,
        [Frozen] SufixoCartao sufixo,
        Cartao existente,
        VencimentosCartoesNovos vencimentos)
    {
        Banco outroBanco = Banco.List.First(b => b != existente.Banco);

        IReadOnlyList<Cartao> result = CartaoFactory.ObterOuCriar(idUsuario, [existente], outroBanco, [sufixo], vencimentos);

        Cartao cartao = result.Should().ContainSingle().Subject;
        cartao.Should().NotBe(existente);
        cartao.Banco.Should().Be(outroBanco);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_FinalRepetido_Deve_DevolverUmCartaoPorFinalNaOrdemDosFinais(
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao primeiro,
        SufixoCartao segundo,
        VencimentosCartoesNovos vencimentos)
    {
        IReadOnlyList<Cartao> result = CartaoFactory.ObterOuCriar(
            idUsuario, cartoesDoUsuario: [], banco, [primeiro, segundo, primeiro], vencimentos);

        result.Select(c => c.Sufixo).Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void ObterOuCriar_Quando_ListaComCartaoDeOutroUsuario_Deve_LancarExcecao(
        IdUsuario idUsuario,
        Cartao cartaoDeOutroUsuario,
        Banco banco,
        SufixoCartao sufixo,
        VencimentosCartoesNovos vencimentos)
    {
        Action act = () => CartaoFactory.ObterOuCriar(idUsuario, [cartaoDeOutroUsuario], banco, [sufixo], vencimentos);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
