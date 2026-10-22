using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class LoteTransacoesFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ContaDoUsuario_Deve_CriarUmaTransacaoPorLancamentoNaOrdem(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, [primeiro, segundo]);

        IReadOnlyList<Transacao> transacoes = result.Should().Succeed().Subject;
        transacoes.Select(t => t.Descricao).Should().Equal(primeiro.Descricao, segundo.Descricao);
        transacoes.Should().AllSatisfy(t => t.Origem.Should().Be(origem));
        transacoes.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.CalcularLote(conta.Banco, origem, [primeiro.Identidade, segundo.Identidade]));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_CopiarOsCamposDoLancamentoParaATransacao(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Data.Should().Be(lancamento.Data);
        transacao.Hora.Should().Be(lancamento.Hora);
        transacao.Valor.Should().Be(lancamento.Valor);
        transacao.Descricao.Should().Be(lancamento.Descricao);
        transacao.Contraparte.Should().Be(lancamento.Contraparte);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaCadaTransacao(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.Id.Value.Version.Should().Be(7);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CartaoDoUsuario_Deve_UsarOBancoDoCartaoNaChave(
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        LancamentoExtrato lancamento)
    {
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, cartao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Origem.Should().Be(origem);
        transacao.Chave.Should().Be(ChaveDeduplicacao.Calcular(cartao.Banco, origem, lancamento.Identidade, 1));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentosIdenticosSemIdExterno_Deve_GerarUmaTransacaoParaCada(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, [lancamento, lancamento]);

        result.Should().Succeed()
            .Which.Select(t => t.Chave).Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ContaNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        ContaBancaria contaDeOutroUsuario,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, contaDeOutroUsuario, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CartaoNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        Cartao cartaoDeOutroUsuario,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, cartaoDeOutroUsuario, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoNoLote_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        IdentidadeLancamento identidade,
        DateOnly data,
        Reais valor1,
        Reais valor2,
        TextoObrigatorio descricao,
        LancamentoExtrato outro)
    {
        LancamentoExtrato primeiro = LancamentoExtrato.Criar(data, hora: null, valor1, descricao, contraparte: null, identidade);
        LancamentoExtrato segundo = LancamentoExtrato.Criar(data, hora: null, valor2, descricao, contraparte: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, [outro, primeiro, outro, segundo]);

        ErroIdentificadorRepetido erro = result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>().Subject;
        erro.Posicoes.Should().Equal(2, 4);
        erro.Descricao.Should().Be("Os lançamentos 2 e 4 do lote têm o mesmo identificador atribuído pelo banco do extrato.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LoteVazio_Deve_RetornarListaVazia(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, []);

        result.Should().Succeed().Which.Should().BeEmpty();
    }
}
