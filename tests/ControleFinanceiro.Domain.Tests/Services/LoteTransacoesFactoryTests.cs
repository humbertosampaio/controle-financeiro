using AutoFixture.Xunit2;
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
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        LancamentoExtrato primeiro,
        LancamentoExtrato segundo)
    {
        usuario.AdicionarConta(conta).Should().Succeed();

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [primeiro, segundo]);

        IReadOnlyList<Transacao> transacoes = result.Should().Succeed().Subject;
        transacoes.Select(t => t.Descricao).Should().Equal(primeiro.Descricao, segundo.Descricao);
        transacoes.Should().AllSatisfy(t => t.Origem.Should().Be(origem));
        transacoes.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.CalcularLote(conta.Banco, origem, [primeiro.Identidade, segundo.Identidade]));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_CopiarOsCamposDoLancamentoParaATransacao(
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        LancamentoExtrato lancamento)
    {
        usuario.AdicionarConta(conta).Should().Succeed();

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Data.Should().Be(lancamento.Data);
        transacao.Hora.Should().Be(lancamento.Hora);
        transacao.Valor.Should().Be(lancamento.Valor);
        transacao.Descricao.Should().Be(lancamento.Descricao);
        transacao.Contraparte.Should().Be(lancamento.Contraparte);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaCadaTransacao(
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        LancamentoExtrato lancamento)
    {
        usuario.AdicionarConta(conta).Should().Succeed();

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.Id.Value.Version.Should().Be(7);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CartaoDoUsuario_Deve_UsarOBancoDoCartaoNaChave(
        Usuario usuario,
        Cartao cartao,
        LancamentoExtrato lancamento)
    {
        usuario.AdicionarCartao(cartao).Should().Succeed();
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.Chave.Should().Be(ChaveDeduplicacao.Calcular(cartao.Banco, origem, lancamento.Identidade, 1));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentosIdenticosSemIdExterno_Deve_GerarUmaTransacaoParaCada(
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        LancamentoExtrato lancamento)
    {
        usuario.AdicionarConta(conta).Should().Succeed();

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [lancamento, lancamento]);

        result.Should().Succeed()
            .Which.Select(t => t.Chave).Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ContaNaoPertenceAoUsuario_Deve_RetornarFalha(
        Usuario usuario,
        OrigemTransacao origem,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [lancamento]);

        result.Should().FailWith(Erros.LoteTransacoes.OrigemNaoPertenceAoUsuario);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CartaoNaoPertenceAoUsuario_Deve_RetornarFalha(
        Usuario usuario,
        IdCartao idCartao,
        LancamentoExtrato lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
            usuario, OrigemTransacao.DoCartao(idCartao), [lancamento]);

        result.Should().FailWith(Erros.LoteTransacoes.OrigemNaoPertenceAoUsuario);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoNoLote_Deve_RetornarFalha(
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem,
        IdentidadeLancamento identidade,
        DateOnly data,
        Reais valor1,
        Reais valor2,
        TextoObrigatorio descricao,
        LancamentoExtrato outro)
    {
        usuario.AdicionarConta(conta).Should().Succeed();
        LancamentoExtrato primeiro = LancamentoExtrato.Criar(data, hora: null, valor1, descricao, contraparte: null, identidade);
        LancamentoExtrato segundo = LancamentoExtrato.Criar(data, hora: null, valor2, descricao, contraparte: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, [outro, primeiro, outro, segundo]);

        Erro erro = result.Should().Fail().Subject;
        erro.Codigo.Should().Be("LoteTransacoes.IdentificadorRepetido");
        erro.Descricao.Should().Be("Os lançamentos 2 e 4 do lote têm o mesmo identificador atribuído pelo banco do extrato.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LoteVazio_Deve_RetornarListaVazia(
        Usuario usuario,
        [Frozen] ContaBancaria conta,
        OrigemTransacao origem)
    {
        usuario.AdicionarConta(conta).Should().Succeed();

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(usuario, origem, []);

        result.Should().Succeed().Which.Should().BeEmpty();
    }
}
