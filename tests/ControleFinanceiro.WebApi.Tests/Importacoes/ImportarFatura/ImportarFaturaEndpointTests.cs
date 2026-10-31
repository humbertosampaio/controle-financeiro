using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

[Collection(ApiCollection.Nome)]
public sealed class ImportarFaturaEndpointTests : IAsyncLifetime
{
    private readonly ApiComPostgresFixture _api;

    public ImportarFaturaEndpointTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    public Task InitializeAsync() => _api.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory, ApiAutoData]
    public async Task Post_Quando_FinaisEAliasesResolvidos_Deve_GravarUmaImportacaoPorCartaoEAsTransacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        ImportarFaturaResponse corpo = (await resposta.Content.ReadFromJsonAsync<ImportarFaturaResponse>())!;
        corpo.Cartoes.Select(c => (SufixoCartao)c.Sufixo).Should().Equal(FaturaCartaoC6.SufixoFisico, FaturaCartaoC6.SufixoVirtual);
        corpo.Cartoes.Should().AllSatisfy(c =>
        {
            c.Lidas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
            c.Novas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
            c.JaExistentes.Should().Be(0);
        });

        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Select(c => c.Sufixo).Should().BeEquivalentTo([FaturaCartaoC6.SufixoFisico, FaturaCartaoC6.SufixoVirtual]);
        cartoes.Should().AllSatisfy(c =>
        {
            c.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
            c.Banco.Should().BeSameAs(Banco.C6);
        });

        Cartao fisico = cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico);
        ImportarFaturaResponse.CartaoImportado linhaFisico = corpo.Cartoes[0];
        ((Guid)fisico.Id).Should().Be(linhaFisico.IdCartao);

        List<ImportacaoArquivo> importacoes = await _api.ObterTodosAsync(db => db.Importacoes);
        importacoes.Should().HaveCount(2);
        ImportacaoArquivo importacaoFisico = importacoes.Single(i => i.Origem == OrigemTransacao.DoCartao(fisico.Id));
        ((Guid)importacaoFisico.Id).Should().Be(linhaFisico.IdImportacao);
        importacaoFisico.Layout.Should().BeSameAs(LayoutArquivoImportacao.C6CartaoCsv);
        importacaoFisico.Periodo.Should().Be((PeriodoArquivo)(FaturaCartaoC6.PrimeiraCompraFisico, FaturaCartaoC6.UltimaCompraFisico));
        importacaoFisico.Lidas.Should().Be(FaturaCartaoC6.LancamentosPorCartao);

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos);
        transacoes.Where(t => t.Origem == OrigemTransacao.DoCartao(fisico.Id)).Should().AllSatisfy(t =>
        {
            t.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
            t.IdImportacao.Should().Be(importacaoFisico.Id);
        });
        transacoes.Sum(t => t.Valor).Should().Be(213.75m);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_GravarAParcelaEACategoriaDoBanco(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        Transacao parcelada = transacoes.Should().ContainSingle(t => t.EhParcelada).Subject;
        parcelada.Parcela.Should().Be((Parcela)(3, 10));
        parcelada.Data.Should().Be(FaturaCartaoC6.PrimeiraCompraFisico);
        parcelada.CategoriaBanco.Should().Be("Eletrônicos");
        transacoes.Should().ContainSingle(t => t.CategoriaBanco == null);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_DeixarONomeDoTitularForaDoQueEGravado(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().AllSatisfy(t =>
        {
            t.Descricao.Value.Should().NotContain(FaturaCartaoC6.TitularFicticio);
            t.Contraparte.Should().BeNull();
            t.CategoriaBanco.Should().NotBe(FaturaCartaoC6.TitularFicticio);
        });
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CartaoJaCadastrado_Deve_ReaproveitarOCartaoSemMudarOVencimento(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        AdicionarCartaoRequest fisico = cartao with { Banco = Banco.C6, Sufixo = FaturaCartaoC6.SufixoFisico, DiaVencimento = 15 };
        Guid idCartao = await _api.AdicionarCartaoAsync(idUsuario, fisico);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes[0].IdCartao.Should().Be(idCartao);

        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Should().HaveCount(2);
        cartoes.Single(c => c.Id == IdCartao.From(idCartao)).DiaVencimento.Should().Be((DiaVencimentoCartao)15);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_OutroUsuarioTemOMesmoCartao_Deve_UsarOCartaoDoUsuario(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest cadastroOutro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutro = await _api.CadastrarUsuarioAsync(cadastroOutro);
        await ResolverPendenciasAsync(idOutro, FaturaCartaoC6.Completo());
        ImportarFaturaResponse doOutro = await ImportarComSucessoAsync(idOutro, FaturaCartaoC6.Completo());
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Should().AllSatisfy(c => c.Novas.Should().Be(FaturaCartaoC6.LancamentosPorCartao));
        List<Cartao> cartoes = await _api.ObterTodosAsync(db => db.Cartoes);
        cartoes.Where(c => c.IdUsuario == IdUsuario.From(idUsuario)).Select(c => (Guid)c.Id)
            .Should().BeEquivalentTo(corpo.Cartoes.Select(c => c.IdCartao));
        cartoes.Where(c => c.IdUsuario == IdUsuario.From(idOutro)).Select(c => (Guid)c.Id)
            .Should().BeEquivalentTo(doOutro.Cartoes.Select(c => c.IdCartao));
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(2 * FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ReimportaAMesmaFatura_Deve_ManterAsTransacoesERegistrarAsNovasImportacoes(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        ImportarFaturaResponse primeira = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse segunda = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        segunda.Cartoes.Should().AllSatisfy(c =>
        {
            c.Novas.Should().Be(0);
            c.JaExistentes.Should().Be(FaturaCartaoC6.LancamentosPorCartao);
        });
        segunda.Cartoes.Select(c => c.IdCartao).Should().Equal(primeira.Cartoes.Select(c => c.IdCartao));
        (await _api.ObterTodosAsync(db => db.Transacoes)).Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos);
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().HaveCount(2);
        (await _api.ObterTodosAsync(db => db.Importacoes)).Should().HaveCount(4);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ParcelaSeguinteNaFaturaSeguinte_Deve_GravarUmaTransacaoParaCadaParcela(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        corpo.Cartoes[0].Novas.Should().Be(1);
        corpo.Cartoes[1].Novas.Should().Be(0);
        List<Transacao> transacoes = await _api.ObterTodosAsync(db => db.Transacoes);
        transacoes.Should().HaveCount(FaturaCartaoC6.QuantidadeLancamentos + 1);
        transacoes.Where(t => t.EhParcelada).Select(t => t.Parcela).Should().BeEquivalentTo([(Parcela)(3, 10), (Parcela)(4, 10)]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ArquivoDoExtrato_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, ExtratoContaC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FinalSemCartao_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverAliasesAsync(idUsuario, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().BeEmpty();
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FinalSemCartaoEAliasesPendentes_Deve_RecusarPeloCartao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ProblemDetails? problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        problema!.Errors.Should().ContainSingle().Which.Reason.Should().Be(ErroCartoesNaoResolvidos.Instancia.Descricao);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SoUmFinalComCartao_Deve_RetornarErroNoArquivoSemGravar(
        CadastrarUsuarioRequest cadastro,
        AdicionarCartaoRequest cartao)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await _api.AdicionarCartaoAsync(idUsuario, cartao with { Banco = Banco.C6, Sufixo = FaturaCartaoC6.SufixoFisico });
        await ResolverAliasesAsync(idUsuario, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        (await _api.ObterTodosAsync(db => db.Cartoes)).Should().ContainSingle();
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_SoOutroUsuarioTemOCartao_Deve_RetornarErroNoArquivoSemGravar(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest cadastroOutro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutro = await _api.CadastrarUsuarioAsync(cadastroOutro);
        await _api.ResolverCartoesDaFaturaAsync(idOutro, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());
        await ResolverAliasesAsync(idUsuario, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CartoesCadastradosDepoisDaRecusa_Deve_Importar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverAliasesAsync(idUsuario, FaturaCartaoC6.Completo());
        using HttpResponseMessage recusa = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());
        recusa.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Sum(c => c.Novas).Should().Be(FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiarioSemAlias_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_BeneficiariosResolvidosDepoisDaRecusa_Deve_Importar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());
        using HttpResponseMessage recusa = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());
        recusa.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Sum(c => c.Novas).Should().Be(FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriaBancoSemAlias_Deve_RetornarErroNoArquivoSemGravar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriasResolvidasDepoisDaRecusa_Deve_Importar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());
        using HttpResponseMessage recusa = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());
        recusa.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await _api.ResolverCategoriasDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Sum(c => c.Novas).Should().Be(FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_OutroUsuarioResolveuAsCategorias_Deve_RecusarOArquivo(
        CadastrarUsuarioRequest cadastro,
        CadastrarUsuarioRequest cadastroOutro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutro = await _api.CadastrarUsuarioAsync(cadastroOutro);
        await _api.ResolverCategoriasDaFaturaAsync(idOutro, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await resposta.CamposComErroAsync()).Should().Equal("arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_CategoriasAssociadasAOutros_Deve_Importar(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutros = (await _api.ObterTodosAsync(db => db.Categorias)).Single(c => c.EhOutros).Id.Value;
        await ResolverCartoesAsync(idUsuario, FaturaCartaoC6.Completo());
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo());
        await _api.AssociarCategoriasDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, FaturaCartaoC6.Completo(), idOutros);

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Sum(c => c.Novas).Should().Be(FaturaCartaoC6.QuantidadeLancamentos);
    }

    [Theory, ApiAutoData]
    public async Task Post_Deve_CriarADespesaParceladaDaCompra(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Select(c => c.DespesasParceladasCriadas).Should().Equal(1, 0);

        DespesaParcelada despesa = (await ObterParceladasAsync()).Should().ContainSingle().Subject;
        Cartao fisico = (await _api.ObterTodosAsync(db => db.Cartoes)).Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico);
        Beneficiario beneficiario = (await _api.ObterTodosAsync(db => db.Beneficiarios)).Single(b => b.PossuiAlias((Alias)"LOJA DE EXEMPLO"));
        CategoriaDespesa categoria = (await _api.ObterTodosAsync(db => db.Categorias)).Single(c => c.PossuiAlias((Alias)"Eletrônicos"));

        despesa.IdUsuario.Should().Be(IdUsuario.From(idUsuario));
        despesa.IdCartao.Should().Be(fisico.Id);
        despesa.IdBeneficiario.Should().Be(beneficiario.Id);
        despesa.IdCategoria.Should().Be(categoria.Id);
        despesa.DataOcorrencia.Should().Be(FaturaCartaoC6.PrimeiraCompraFisico);

        // O cartão vence no dia 10 e fecha no dia 3: a compra de 15/07/2025 cai na fatura de 08/2025.
        ParcelaDespesa[] parcelas = despesa.Parcelas.OrderBy(p => p.NumeroParcela).ToArray();
        parcelas.Select(p => p.NumeroParcela).Should().Equal(Enumerable.Range(1, 10));
        parcelas[0].MesReferencia.Should().Be((MesReferencia)(08, 2025));
        parcelas[0].Pagamento.DataProjetada.Should().Be(new DateOnly(2025, 08, 10));
        parcelas[9].MesReferencia.Should().Be((MesReferencia)(05, 2026));
        parcelas.Should().OnlyContain(p => p.Pagamento.Valor == (ReaisPositivo)99.90m);
        parcelas.Take(2).Should().OnlyContain(p => p.Pagamento.EstaPago);
        parcelas.Skip(2).Should().OnlyContain(p => !p.Pagamento.EstaPago);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_Reimportacao_Deve_NaoDuplicarADespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        corpo.Cartoes.Should().OnlyContain(c => c.DespesasParceladasCriadas == 0);
        (await ObterParceladasAsync()).Should().ContainSingle();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturaSeguinte_Deve_NaoCriarNemAlterarADespesa(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        corpo.Cartoes.Should().OnlyContain(c => c.DespesasParceladasCriadas == 0);
        DespesaParcelada despesa = (await ObterParceladasAsync()).Should().ContainSingle().Subject;
        despesa.Parcelas.Single(p => p.NumeroParcela == 3).Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_FaturasForaDeOrdem_Deve_ManterADespesaDaPrimeiraImportada(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComParcelaSeguinte());

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());

        DespesaParcelada despesa = (await ObterParceladasAsync()).Should().ContainSingle().Subject;
        despesa.Parcelas.Where(p => p.Pagamento.EstaPago).Select(p => p.NumeroParcela).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_ComprasGemeas_Deve_CriarUmaDespesaParaCadaSemDuplicarNaReimportacao(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());

        ImportarFaturaResponse corpo = await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComCompraParceladaGemea());
        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.ComCompraParceladaGemea());

        corpo.Cartoes[0].DespesasParceladasCriadas.Should().Be(2);
        (await ObterParceladasAsync()).Should().HaveCount(2);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_DoisUsuariosComAMesmaFatura_Deve_CriarUmaDespesaParaCada(CadastrarUsuarioRequest cadastro, CadastrarUsuarioRequest outroCadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);
        Guid idOutro = await _api.CadastrarUsuarioAsync(outroCadastro);
        await ResolverPendenciasAsync(idUsuario, FaturaCartaoC6.Completo());
        await ResolverPendenciasAsync(idOutro, FaturaCartaoC6.Completo());

        await ImportarComSucessoAsync(idUsuario, FaturaCartaoC6.Completo());
        await ImportarComSucessoAsync(idOutro, FaturaCartaoC6.Completo());

        (await ObterParceladasAsync()).Select(d => d.IdUsuario).Should().BeEquivalentTo([IdUsuario.From(idUsuario), IdUsuario.From(idOutro)]);
    }

    [Theory, ApiAutoData]
    public async Task Post_Quando_UsuarioNaoExiste_Deve_RetornarNaoEncontradoSemGravar(Guid idUsuario)
    {
        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, FaturaCartaoC6.Completo());

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _api.DeveEstarSemImportacaoAsync();
    }

    // A leitura do formulário para no limite da rota, antes do validator: o erro é geral, e não no campo do arquivo.
    [Theory, ApiAutoData]
    public async Task Post_Quando_FormularioAcimaDoLimiteDaRota_Deve_RecusarSemLerOArquivoInteiro(CadastrarUsuarioRequest cadastro)
    {
        Guid idUsuario = await _api.CadastrarUsuarioAsync(cadastro);

        using HttpResponseMessage resposta = await ImportarAsync(idUsuario, new byte[ImportarFaturaEndpoint.TamanhoMaximoFormulario + 1]);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().Equal("generalErrors");
        await _api.DeveEstarSemImportacaoAsync();
    }

    [Fact]
    public async Task Post_Quando_RequisicaoInvalida_Deve_RetornarErroEmCadaCampo()
    {
        // O formulário multipart vazio não chega ao validator; o campo a mais só o deixa não vazio.
        using MultipartFormDataContent formulario = new() { { new StringContent("ignorado"), "outro" } };

        using HttpResponseMessage resposta = await _api.Client.PostAsync(ImportacaoFaturaExtensions.RotaFatura(Guid.Empty, layout: "layout-desconhecido"), formulario);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await resposta.CamposComErroAsync()).Should().BeEquivalentTo("idUsuario", "layout", "arquivo");
        await _api.DeveEstarSemImportacaoAsync();
    }

    private Task<List<DespesaParcelada>> ObterParceladasAsync() => _api.ObterTodosAsync(db => db.Despesas.OfType<DespesaParcelada>());

    private Task<HttpResponseMessage> ImportarAsync(Guid idUsuario, byte[] arquivo)
    {
        return _api.ImportarFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
    }

    private Task<ImportarFaturaResponse> ImportarComSucessoAsync(Guid idUsuario, byte[] arquivo)
    {
        return _api.ImportarFaturaComSucessoAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
    }

    private Task ResolverCartoesAsync(Guid idUsuario, byte[] arquivo)
    {
        return _api.ResolverCartoesDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
    }

    private async Task ResolverPendenciasAsync(Guid idUsuario, byte[] arquivo)
    {
        await ResolverCartoesAsync(idUsuario, arquivo);
        await ResolverAliasesAsync(idUsuario, arquivo);
    }

    private async Task ResolverAliasesAsync(Guid idUsuario, byte[] arquivo)
    {
        await _api.ResolverBeneficiariosDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
        await _api.ResolverCategoriasDaFaturaAsync(idUsuario, LayoutArquivoImportacao.C6CartaoCsv, arquivo);
    }
}
