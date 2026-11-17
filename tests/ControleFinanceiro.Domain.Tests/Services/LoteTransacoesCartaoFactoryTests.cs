using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class LoteTransacoesCartaoFactoryTests
{
    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_LancamentoComParcela_Deve_CriarTransacaoParcelada(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        [Frozen] Parcela parcela,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Parcela.Should().Be(parcela);
        transacao.EhParcelada.Should().BeTrue();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoDoUsuario_Deve_CopiarOMesDaFaturaDaImportacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        Lancamento primeiro,
        Lancamento segundo)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [primeiro, segundo]);

        result.Should().Succeed().Which.Should().OnlyContain(t => t.MesVencimentoFatura == importacao.MesVencimento);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoDoUsuario_Deve_UsarOBancoDoCartaoNaChave(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        Lancamento lancamento)
    {
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Origem.Should().Be(origem);
        transacao.Chave.Should().Be(ChaveDeduplicacao.Calcular(cartao.Banco, origem, lancamento.Identidade, ordinal: 1));
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_PagamentoFatura_Deve_CopiarAMarcaParaATransacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento.ComoPagamentoFatura()]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.EhPagamentoFatura.Should().BeTrue();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_SoAMarcaDePagamentoFaturaDifere_Deve_GerarAMesmaChave(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        Lancamento lancamento)
    {
        Transacao semMarca = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento]).Value.Single();

        Transacao comMarca = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento.ComoPagamentoFatura()]).Value.Single();

        comMarca.Chave.Should().Be(semMarca.Chave);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_IdentidadesCompostasPeloMesmoIdentificador_Deve_GerarUmaTransacaoParaCadaPeloOrdinal(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        string identificador,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao)
    {
        IdentidadeLancamento identidade = IdentidadeLancamento.Composta(identificador).Value.ComValor(valor);
        Lancamento lancamento = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento, lancamento]);

        result.Should().Succeed().Which.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.Calcular(cartao.Banco, origem, identidade, ordinal: 1),
            ChaveDeduplicacao.Calcular(cartao.Banco, origem, identidade, ordinal: 2));
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        [Frozen] Cartao cartaoDeOutroUsuario,
        ImportacaoFatura importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartaoDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_ImportacaoDeOutroCartao_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        ImportacaoFatura importacaoDeOutroCartao,
        Lancamento lancamento)
    {
        Action criar = () => CriarComAliasesResolvidos(idUsuario, cartao, importacaoDeOutroCartao, [lancamento]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("importacao");
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_SaidaNoCartao_Deve_TerADescricaoComoTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor, estabelecimento, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [compra]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().Be(estabelecimento);
    }

    /// <remarks>O estorno e o pagamento da fatura entram no cartão com valor positivo.</remarks>
    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_EntradaNoCartaoComoEstornoOuPagamentoFatura_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao)
    {
        Lancamento entrada = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [entrada]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_DescricaoDaCompraSemAlias_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor, estabelecimento, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesCartaoFactory.Criar(
            idUsuario,
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            cartao,
            importacao,
            [compra]);

        result.Should().FailWith(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_LinhaDeExtratoNoCartao_Deve_Lancar(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoFatura importacao,
        Lancamento lancamento)
    {
        Lancamento doExtrato = lancamento.NaOrigem(TipoOrigem.Conta);

        Action criar = () => LoteTransacoesCartaoFactory.Criar(idUsuario, beneficiariosDoUsuario: [], categoriasDoUsuario: [], cartao, importacao, [doExtrato]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("lancamentos");
    }

    /// <summary>
    /// Cria o lote com um beneficiário para cada texto de beneficiário e uma categoria para cada categoria do banco dos
    /// lançamentos, para não falhar por isso.
    /// </summary>
    private static Result<IReadOnlyList<Transacao>, Erro> CriarComAliasesResolvidos(
        IdUsuario idUsuario,
        Cartao cartao,
        ImportacaoFatura importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(idUsuario, lancamentos);
        return LoteTransacoesCartaoFactory.Criar(idUsuario, beneficiarios, categorias, cartao, importacao, lancamentos);
    }

    /// <summary>
    /// Monta os lançamentos gerados como linhas de fatura.
    /// </summary>
    private sealed class ImportacaoDoCartaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public ImportacaoDoCartaoAutoDataAttribute()
            : base(fixture => fixture.Inject(TipoOrigem.Cartao))
        {
        }
    }
}
