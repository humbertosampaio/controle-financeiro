using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
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

public sealed class LoteTransacoesContaFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ContaDoUsuario_Deve_CriarUmaTransacaoPorLancamentoNaOrdem(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        OrigemTransacao origem,
        LancamentoConta primeiro,
        LancamentoConta segundo)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [primeiro, segundo]);

        IReadOnlyList<TransacaoConta> transacoes = result.Should().Succeed().Subject;
        transacoes.Select(t => t.Descricao).Should().Equal(primeiro.Descricao, segundo.Descricao);
        transacoes.Should().AllSatisfy(t => t.IdConta.Should().Be(conta.Id));
        transacoes.Select(t => t.Chave).Should().Equal(
            ChaveDeduplicacao.CalcularLote(conta.Banco, origem, [primeiro.Identidade, segundo.Identidade]));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_CopiarOsCamposDoLancamentoParaATransacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        TransacaoConta transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Data.Should().Be(lancamento.Data);
        transacao.Hora.Should().Be(lancamento.Hora);
        transacao.Valor.Should().Be(lancamento.Valor);
        transacao.Descricao.Should().Be(lancamento.Descricao);
        transacao.Contraparte.Should().Be(lancamento.Contraparte);
        transacao.CategoriaBanco.Should().Be(lancamento.CategoriaBanco);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_LigarCadaTransacaoAoUsuarioEAImportacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta primeiro,
        LancamentoConta segundo)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [primeiro, segundo]);

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
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.Id.Value.Version.Should().Be(7);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentosIdenticosSemIdExterno_Deve_GerarUmaTransacaoParaCada(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento, lancamento]);

        result.Should().Succeed()
            .Which.Select(t => t.Chave).Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ContaNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        [Frozen] ContaBancaria contaDeOutroUsuario,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, contaDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ImportacaoDeOutraConta_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacaoDeOutraConta,
        LancamentoConta lancamento)
    {
        Action criar = () => CriarComAliasesResolvidos(idUsuario, conta, importacaoDeOutraConta, [lancamento]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("importacao");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoNoLote_Deve_RetornarFalha(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        Reais valor1,
        Reais valor2,
        TextoObrigatorio descricao,
        LancamentoConta outro)
    {
        LancamentoConta primeiro = LancamentoConta.Criar(data, hora: null, valor1, descricao, contraparte: null, categoriaBanco: null, identidade);
        LancamentoConta segundo = LancamentoConta.Criar(data, hora: null, valor2, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [outro, primeiro, outro, segundo]);

        ErroIdentificadorRepetido erro = result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>().Subject;
        erro.Posicoes.Should().Equal(2, 4);
        erro.Descricao.Should().Be("Os lançamentos 2 e 4 do lote têm o mesmo identificador atribuído pelo banco do extrato.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SaidaNaContaComContraparte_Deve_TerAContraparteComoTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte)
    {
        LancamentoConta saida = LancamentoConta.Criar(data, hora: null, -valor, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [saida]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SaidaNaContaSemContraparte_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao)
    {
        LancamentoConta boleto = LancamentoConta.Criar(data, hora: null, -valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [boleto]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_EntradaNaConta_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte)
    {
        LancamentoConta entrada = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [entrada]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory]
    [InlineDomainAutoData(-0.01, true)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, false)]
    public void Criar_Deve_TerSaidaSoComValorNegativo(
        decimal valor,
        bool esperado,
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao)
    {
        LancamentoConta lancamento = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.EhSaida.Should().Be(esperado);
    }

    [Theory]
    [InlineDomainAutoData(-0.01, false)]
    [InlineDomainAutoData(0, false)]
    [InlineDomainAutoData(0.01, true)]
    public void Criar_Deve_TerEntradaSoComValorPositivo(
        decimal valor,
        bool esperado,
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao)
    {
        LancamentoConta lancamento = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.EhEntrada.Should().Be(esperado);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ValorZeroNaConta_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte)
    {
        LancamentoConta ajuste = LancamentoConta.Criar(data, hora: null, valor: 0m, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [ajuste]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_TextoBeneficiarioSemAlias_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        [Frozen] Alias alias,
        Beneficiario beneficiario,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        IdentidadeLancamento outraIdentidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias)
    {
        LancamentoConta resolvida = LancamentoConta.Criar(data, hora: null, -valor, descricao, alias.TextoOriginal, categoriaBanco: null, identidade);
        LancamentoConta pendente = LancamentoConta.Criar(data, hora: null, -valor, descricao, semAlias, categoriaBanco: null, outraIdentidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = LoteTransacoesContaFactory.Criar(
            idUsuario,
            [beneficiario],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [resolvida, pendente]);

        result.Should().FailWith(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoEBeneficiarioSemAlias_Deve_RetornarOIdentificadorRepetido(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias)
    {
        LancamentoConta saida = LancamentoConta.Criar(data, hora: null, -valor, descricao, semAlias, categoriaBanco: null, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = LoteTransacoesContaFactory.Criar(
            idUsuario,
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [saida, saida]);

        result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ListaComBeneficiarioDeOutroUsuario_Deve_LancarExcecao(
        Beneficiario beneficiarioDeOutroUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        Action criar = () => LoteTransacoesContaFactory.Criar(
            conta.IdUsuario,
            [beneficiarioDeOutroUsuario],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [lancamento]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CategoriaBancoSemAlias_Deve_RetornarCategoriasBancoNaoResolvidas(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, [lancamento]);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = LoteTransacoesContaFactory.Criar(
            idUsuario,
            beneficiarios,
            categoriasDoUsuario: [],
            conta,
            importacao,
            [lancamento]);

        result.Should().FailWith(ErroCategoriasBancoNaoResolvidas.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_BeneficiarioECategoriaPendentes_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias,
        string categoriaBanco)
    {
        LancamentoConta saida = LancamentoConta.Criar(data, hora: null, -valor, descricao, semAlias, categoriaBanco, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = LoteTransacoesContaFactory.Criar(
            idUsuario,
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [saida]);

        result.Should().FailWith(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdentificadorRepetidoECategoriaPendente_Deve_RetornarIdentificadorRepetido(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string categoriaBanco)
    {
        LancamentoConta entrada = LancamentoConta.Criar(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        Result<IReadOnlyList<TransacaoConta>, Erro> result = LoteTransacoesContaFactory.Criar(
            idUsuario,
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [entrada, entrada]);

        result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ListaComCategoriaDeOutroUsuario_Deve_LancarExcecao(
        CategoriaDespesa categoriaDeOutroUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao,
        LancamentoConta lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(conta.IdUsuario, [lancamento]);

        Action criar = () => LoteTransacoesContaFactory.Criar(
            conta.IdUsuario,
            beneficiarios,
            [categoriaDeOutroUsuario],
            conta,
            importacao,
            [lancamento]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LoteVazio_Deve_RetornarListaVazia(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoExtrato importacao)
    {
        Result<IReadOnlyList<TransacaoConta>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, lancamentos: []);

        result.Should().Succeed()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// Cria o lote com um beneficiário para cada texto de beneficiário e uma categoria para cada categoria do banco dos
    /// lançamentos, para não falhar por isso.
    /// </summary>
    private static Result<IReadOnlyList<TransacaoConta>, Erro> CriarComAliasesResolvidos(
        IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoConta> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(idUsuario, lancamentos);
        return LoteTransacoesContaFactory.Criar(idUsuario, beneficiarios, categorias, conta, importacao, lancamentos);
    }
}
