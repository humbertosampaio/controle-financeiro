using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
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
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [primeiro, segundo]);

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
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Data.Should().Be(lancamento.Data);
        transacao.Hora.Should().Be(lancamento.Hora);
        transacao.Valor.Should().Be(lancamento.Valor);
        transacao.Descricao.Should().Be(lancamento.Descricao);
        transacao.Contraparte.Should().Be(lancamento.Contraparte);
        transacao.Parcela.Should().Be(lancamento.Parcela);
        transacao.CategoriaBanco.Should().Be(lancamento.CategoriaBanco);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_LancamentoComParcela_Deve_CriarTransacaoParcelada(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        [Frozen] Parcela parcela,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento]);

        Transacao transacao = result.Should().Succeed().Which.Should().ContainSingle().Subject;
        transacao.Parcela.Should().Be(parcela);
        transacao.EhParcelada.Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentoSemParcela_Deve_CriarTransacaoNaoParcelada(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        Reais valor,
        TextoObrigatorio descricao)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed().Which.Should().ContainSingle().Which.EhParcelada.Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_LigarCadaTransacaoAoUsuarioEAImportacao(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento primeiro,
        Lancamento segundo)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [primeiro, segundo]);

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
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

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

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento]);

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
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento, lancamento]);

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
        Lancamento lancamento = Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela: null, categoriaBanco: null, identidade);
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [lancamento, lancamento]);

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
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, contaDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_CartaoNaoPertenceAoUsuario_Deve_RetornarFalha(
        IdUsuario idUsuario,
        [Frozen] Cartao cartaoDeOutroUsuario,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartaoDeOutroUsuario, importacao, [lancamento]);

        result.Should().FailWith(ErroOrigemNaoPertenceAoUsuario.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ImportacaoDeOutraOrigem_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoArquivo importacaoDeOutraConta,
        Lancamento lancamento)
    {
        Action criar = () => CriarComAliasesResolvidos(idUsuario, conta, importacaoDeOutraConta, [lancamento]);

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
        Lancamento primeiro = Lancamento.DaConta(data, hora: null, valor1, descricao, contraparte: null, categoriaBanco: null, identidade);
        Lancamento segundo = Lancamento.DaConta(data, hora: null, valor2, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [outro, primeiro, outro, segundo]);

        ErroIdentificadorRepetido erro = result.Should().Fail().Which.Should().BeOfType<ErroIdentificadorRepetido>().Subject;
        erro.Posicoes.Should().Equal(2, 4);
        erro.Descricao.Should().Be("Os lançamentos 2 e 4 do lote têm o mesmo identificador atribuído pelo banco do extrato.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SaidaNaContaComContraparte_Deve_TerAContraparteComoTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte)
    {
        Lancamento saida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [saida]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().Be(contraparte);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_SaidaNaContaSemContraparte_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao)
    {
        Lancamento boleto = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [boleto]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_EntradaNaConta_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string contraparte)
    {
        Lancamento entrada = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [entrada]);

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
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

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
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao)
    {
        Lancamento lancamento = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [lancamento]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.EhEntrada.Should().Be(esperado);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ValorZeroNaConta_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        TextoObrigatorio descricao,
        string contraparte)
    {
        Lancamento ajuste = Lancamento.DaConta(data, hora: null, valor: 0m, descricao, contraparte, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, [ajuste]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().BeNull();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_SaidaNoCartao_Deve_TerADescricaoComoTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor.Value, estabelecimento, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, cartao, importacao, [compra]);

        result.Should().Succeed()
            .Which.Should().ContainSingle()
            .Which.TextoBeneficiario.Should().Be(estabelecimento.Value);
    }

    /// <remarks>O estorno e o pagamento da fatura entram no cartão com valor positivo.</remarks>
    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_EntradaNoCartaoComoEstornoOuPagamentoFatura_Deve_FicarSemTextoBeneficiario(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
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

    [Theory, DomainAutoData]
    public void Criar_Quando_TextoBeneficiarioSemAlias_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        [Frozen] Alias alias,
        Beneficiario beneficiario,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        IdentidadeLancamento outraIdentidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias)
    {
        Lancamento resolvida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, alias.TextoOriginal, categoriaBanco: null, identidade);
        Lancamento pendente = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, semAlias, categoriaBanco: null, outraIdentidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
            idUsuario,
            [beneficiario],
            categoriasDoUsuario: [],
            conta,
            importacao,
            [resolvida, pendente]);

        result.Should().FailWith(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_DescricaoDaCompraSemAlias_Deve_RetornarBeneficiariosNaoResolvidos(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio estabelecimento)
    {
        Lancamento compra = Lancamento.DoCartao(data, hora: null, -valor.Value, estabelecimento, contraparte: null, parcela: null, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
            idUsuario,
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            cartao,
            importacao,
            [compra]);

        result.Should().FailWith(ErroBeneficiariosNaoResolvidos.Instancia);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_IdExternoRepetidoEBeneficiarioSemAlias_Deve_RetornarOIdentificadorRepetido(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias)
    {
        Lancamento saida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, semAlias, categoriaBanco: null, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Action criar = () => LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, [lancamento]);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string semAlias,
        string categoriaBanco)
    {
        Lancamento saida = Lancamento.DaConta(data, hora: null, -valor.Value, descricao, semAlias, categoriaBanco, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao,
        IdentidadeLancamento identidade,
        DateOnly data,
        ReaisPositivo valor,
        TextoObrigatorio descricao,
        string categoriaBanco)
    {
        Lancamento entrada = Lancamento.DaConta(data, hora: null, valor, descricao, contraparte: null, categoriaBanco, identidade);

        Result<IReadOnlyList<Transacao>, Erro> result = LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(conta.IdUsuario, [lancamento]);

        Action criar = () => LoteTransacoesFactory.Criar(
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
        ImportacaoArquivo importacao)
    {
        Result<IReadOnlyList<Transacao>, Erro> result = CriarComAliasesResolvidos(idUsuario, conta, importacao, lancamentos: []);

        result.Should().Succeed()
            .Which.Should().BeEmpty();
    }

    [Theory, ImportacaoDoCartaoAutoData]
    public void Criar_Quando_LinhaDeExtratoNoCartao_Deve_Lancar(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Lancamento doExtrato = lancamento.NaOrigem(TipoOrigem.Conta);

        Action criar = () => LoteTransacoesFactory.Criar(idUsuario, beneficiariosDoUsuario: [], categoriasDoUsuario: [], cartao, importacao, [doExtrato]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("lancamentos");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_LancamentoDeOutroTipoDeOrigem_Deve_Lancar(
        [Frozen] IdUsuario idUsuario,
        [Frozen] ContaBancaria conta,
        ImportacaoArquivo importacao,
        Lancamento lancamento)
    {
        Lancamento daFatura = lancamento.NaOrigem(TipoOrigem.Cartao);

        Action criar = () => LoteTransacoesFactory.Criar(idUsuario, beneficiariosDoUsuario: [], categoriasDoUsuario: [], conta, importacao, [daFatura]);

        criar.Should().ThrowExactly<ArgumentException>().WithParameterName("lancamentos");
    }

    /// <summary>
    /// Cria o lote com um beneficiário para cada texto de beneficiário e uma categoria para cada categoria do banco dos
    /// lançamentos, para não falhar por isso.
    /// </summary>
    private static Result<IReadOnlyList<Transacao>, Erro> CriarComAliasesResolvidos(
        IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(idUsuario, lancamentos);
        return LoteTransacoesFactory.Criar(idUsuario, beneficiarios, categorias, conta, importacao, lancamentos);
    }

    /// <summary>
    /// Cria o lote com um beneficiário para cada texto de beneficiário e uma categoria para cada categoria do banco dos
    /// lançamentos, para não falhar por isso.
    /// </summary>
    private static Result<IReadOnlyList<Transacao>, Erro> CriarComAliasesResolvidos(
        IdUsuario idUsuario,
        Cartao cartao,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(idUsuario, lancamentos);
        return LoteTransacoesFactory.Criar(idUsuario, beneficiarios, categorias, cartao, importacao, lancamentos);
    }

    /// <summary>
    /// Monta a <see cref="ImportacaoArquivo"/> a partir de um cartão, e não de uma conta, como faz o generator do
    /// <see cref="DomainAutoDataAttribute"/>, e os lançamentos gerados como linhas de fatura. Com <c>[Frozen] Cartao</c> no
    /// teste, a importação sai desse cartão.
    /// </summary>
    private sealed class ImportacaoDoCartaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public ImportacaoDoCartaoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Inject(TipoOrigem.Cartao);
                fixture.Register(
                    (Cartao cartao, LayoutArquivoImportacao layout, PeriodoArquivo periodo, DateTimeOffset importadaEm) =>
                        ImportacaoArquivoFactory.Registrar(cartao, layout, periodo, fixture.Create<byte>(), importadaEm));
            })
        {
        }
    }
}
