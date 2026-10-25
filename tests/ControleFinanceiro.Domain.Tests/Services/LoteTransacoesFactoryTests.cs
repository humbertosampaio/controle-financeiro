using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
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
        ImportacaoArquivo importacao,
        OrigemTransacao origem,
        Lancamento primeiro,
        Lancamento segundo)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [primeiro, segundo]);

        IReadOnlyList<Transacao> transacoes = result.Should().Succeed().Subject;
        transacoes.Select(t => t.Descricao).Should().Equal(primeiro.Descricao, segundo.Descricao);
        transacoes.Should().AllSatisfy(t => t.Origem.Should().Be(origem));
        transacoes.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.CalcularLote(conta.Banco, origem, [primeiro.Identidade, segundo.Identidade]));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_CopiarOsCamposDoLancamentoParaATransacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Data.Should().Be(lancamento.Data);
        transacao.Hora.Should().Be(lancamento.Hora);
        transacao.Valor.Should().Be(lancamento.Valor);
        transacao.Descricao.Should().Be(lancamento.Descricao);
        transacao.Contraparte.Should().Be(lancamento.Contraparte);
        transacao.Parcela.Should().Be(lancamento.Parcela);
        transacao.CategoriaBanco.Should().Be(lancamento.CategoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_LigarCadaTransacaoAoUsuarioEAImportacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento primeiro,
        Lancamento segundo)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [primeiro, segundo]);

        result.Should().Succeed().Which.Should().AllSatisfy(t =>
        {
            t.IdUsuario.Should().Be(idUsuario);
            t.IdImportacao.Should().Be(importacao.Id);
        });
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaCadaTransacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.Id.Value.Version.Should().Be(7);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoDoUsuario_Deve_UsarOBancoDoCartaoNaChave(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, cartao, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Origem.Should().Be(origem);
        transacao.Chave.Should().Be(ChaveDeduplicacao.Calcular(cartao.Banco, origem, lancamento.Identidade, 1));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentosIdenticosSemIdExterno_Deve_GerarUmaTransacaoParaCada(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [lancamento, lancamento]);

        result.Should().Succeed()
            .Which.Select(t => t.Chave).Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_IdentidadesCompostasPeloMesmoIdentificador_Deve_GerarUmaTransacaoParaCadaPeloOrdinal(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        string identificador,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao)
    {
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(identificador).Value.ComValor(valor);
        Lancamento lancamento = Lancamento.Criar(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, cartao, importacao, [lancamento, lancamento]);

        result.Should().Succeed().Which.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.Calcular(cartao.Banco, origem, identidade, ordinal: 1),
            ChaveDeduplicacao.Calcular(cartao.Banco, origem, identidade, ordinal: 2));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ContaNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        [Frozen] ContaBancaria contaDeOutroUsuario,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, contaDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        [Frozen] Cartao cartaoDeOutroUsuario,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, cartaoDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ImportacaoDeOutraOrigem_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoArquivo importacaoDeOutraConta,
        Lancamento lancamento)
    {
        Action criar = () => LoteTransacoesFactory.Criar(idUsuario, conta, importacaoDeOutraConta, [lancamento]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("importacao");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoNoLote_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        Reais valor1,
        Reais valor2,
        TextoObrigatorio descricao,
        Lancamento outro)
    {
        Lancamento primeiro = Lancamento.Criar(data, hora: null, valor1, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);
        Lancamento segundo = Lancamento.Criar(data, hora: null, valor2, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, [outro, primeiro, outro, segundo]);

        ErroIdentificadorRepetido erro = result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>().Subject;
        erro.Posicoes.Should().Equal(2, 4);
        erro.Descricao.Should().Be("Os lançamentos 2 e 4 do lote têm o mesmo identificador atribuído pelo banco do extrato.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LoteVazio_Deve_RetornarListaVazia(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(idUsuario, conta, importacao, lancamentos: []);

        result.Should().Succeed().Which.Should().BeEmpty();
    }

    /// <summary>
    /// Monta a <see cref="ImportacaoArquivo"/> a partir de um cartão, e não de uma conta, como faz o generator do
    /// <see cref="DomainAutoDataAttribute"/>. Com <c>[Frozen] Cartao</c> no teste, a importação sai desse cartão.
    /// </summary>
    private sealed class ImportacaoDoCartaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public ImportacaoDoCartaoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Register(
                    (Cartao cartao, LayoutArquivoImportacao layout, PeriodoArquivo periodo, DateTimeOffset importadaEm) =>
                        ImportacaoArquivoFactory.Registrar(cartao, layout, periodo, fixture.Create<byte>(), importadaEm));
            })
        {
        }
    }
}
