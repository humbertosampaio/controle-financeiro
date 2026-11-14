using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using CSharpFunctionalExtensions;
using System.Globalization;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class ImportacaoParaModeloTests
{
    [Theory, DomainAutoData]
    public void Serializar_Quando_Extrato_Deve_TerSomenteOsCamposPermitidos(ExtratoImportado importado)
    {
        ExtratoImportado semAvisos = importado with { PeriodoNaoImportado = Maybe<PeriodoArquivo>.None, SomaPeriodo = Result.Success() };

        JsonObject json = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, semAvisos).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo(
            "layout", "banco", "periodo", "conta_nova", "lidas", "novas", "ja_existentes", "resumo");
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_Extrato_Deve_LevarOLayoutOBancoOPeriodoEAsContagens(ExtratoImportado importado)
    {
        JsonObject json = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, importado).Serializar();

        json.Texto("layout").Should().Be(LayoutArquivoImportacao.C6ContaCsv.Value);
        json.Texto("banco").Should().Be(importado.Banco.Name);
        Periodo(json, "periodo").Should().Be((importado.Periodo.Inicio, importado.Periodo.Fim));
        json["conta_nova"]!.GetValue<bool>().Should().Be(importado.ContaNova);
        json["lidas"]!.GetValue<int>().Should().Be(importado.Lidas);
        json["novas"]!.GetValue<int>().Should().Be(importado.Novas);
        json["ja_existentes"]!.GetValue<int>().Should().Be(importado.JaExistentes);
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_ExtratoComPeriodoNaoImportado_Deve_LevarOPeriodo(ExtratoImportado importado, PeriodoArquivo naoImportado)
    {
        JsonObject json = ImportacaoParaModelo
            .DoExtrato(LayoutArquivoImportacao.C6ContaCsv, importado with { PeriodoNaoImportado = naoImportado })
            .Serializar();

        Periodo(json, "periodo_nao_importado").Should().Be((naoImportado.Inicio, naoImportado.Fim));
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_SomaDoPeriodoNaoFecha_Deve_LevarOAvisoFixoSemOsValores(ExtratoImportado importado)
    {
        ExtratoImportado naoFecha = importado with { SomaPeriodo = Result.Failure("A soma de R$ 10,00 não fecha com R$ 12,00.") };

        JsonObject json = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, naoFecha).Serializar();

        json["avisos"]!.AsArray().Select(aviso => aviso!.GetValue<string>()).Should().ContainSingle()
            .Which.Should().StartWith("A soma das transações gravadas no período não fecha").And.NotContain("R$");
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_Fatura_Deve_TerSomenteOsCamposPermitidos(Fatura fatura, FaturaImportada importada)
    {
        FaturaImportada comPagamento = importada with { PagamentoFaturaAnterior = Result.Success() };

        JsonObject json = ImportacaoParaModelo.DaFatura(LayoutArquivoImportacao.C6CartaoCsv, fatura, comPagamento).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo(
            "layout", "banco", "periodo", "cartoes", "lidas", "novas", "ja_existentes", "resumo");
        json["cartoes"]!.AsArray().Should().OnlyContain(cartao => cartao!.AsObject().Select(campo => campo.Key).SequenceEqual(
            new[] { "final", "lidas", "novas", "ja_existentes", "despesas_parceladas", "despesas_adiadas" }));
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_Fatura_Deve_LevarCadaCartaoESomarAsContagens(Fatura fatura, FaturaImportada importada)
    {
        JsonObject json = ImportacaoParaModelo.DaFatura(LayoutArquivoImportacao.C6CartaoCsv, fatura, importada).Serializar();

        json["cartoes"]!.AsArray().Select(cartao => cartao!.AsObject().Texto("final"))
            .Should().Equal(importada.Cartoes.Select(cartao => (string)cartao.Sufixo));
        json["lidas"]!.GetValue<int>().Should().Be(importada.Cartoes.Sum(cartao => cartao.Lidas));
        json["novas"]!.GetValue<int>().Should().Be(importada.Cartoes.Sum(cartao => cartao.Novas));
        json["ja_existentes"]!.GetValue<int>().Should().Be(importada.Cartoes.Sum(cartao => cartao.JaExistentes));
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_Fatura_Deve_LevarOPeriodoDeTodosOsCartoes(Fatura fatura, FaturaImportada importada)
    {
        JsonObject json = ImportacaoParaModelo.DaFatura(LayoutArquivoImportacao.C6CartaoCsv, fatura, importada).Serializar();

        Periodo(json, "periodo").Should().Be((fatura.Periodo.Inicio, fatura.Periodo.Fim));
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_FaturaSemPagamentoAnterior_Deve_LevarOAvisoFixo(Fatura fatura, FaturaImportada importada, string erro)
    {
        FaturaImportada semPagamento = importada with { PagamentoFaturaAnterior = Result.Failure(erro) };

        JsonObject json = ImportacaoParaModelo.DaFatura(LayoutArquivoImportacao.C6CartaoCsv, fatura, semPagamento).Serializar();

        json["avisos"]!.AsArray().Select(aviso => aviso!.GetValue<string>()).Should().Equal(Fatura.AvisoSemPagamentoAnterior);
    }

    [Fact]
    public void Serializar_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = ImportacaoParaModelo.Falha(ErrosParaModelo.Importacao.LayoutInvalido).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
        ErroLido.DoEnvelope(json).Mensagem.Should().Be(ErrosParaModelo.Importacao.LayoutInvalido.Mensagem);
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_FalhaComPendenciasDaFatura_Deve_LevarAsQuantidadesEOsFinaisSemCartao(
        SufixoCartao semCartao,
        Cartao cartao,
        IReadOnlyList<Alias> beneficiarios,
        IReadOnlyList<Alias> categorias)
    {
        PendenciasFatura pendencias = new(
            [new PendenciasFatura.FinalCartao(semCartao, Maybe<Cartao>.None), new PendenciasFatura.FinalCartao(cartao.Sufixo, cartao)],
            beneficiarios,
            categorias);

        JsonObject json = ImportacaoParaModelo
            .Falha(ErrosParaModelo.Importacao.CartoesNaoResolvidos, PendenciasImportacaoParaModelo.DaFatura(pendencias))
            .Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("erro", "pendencias");
        JsonObject lidas = json["pendencias"]!.AsObject();
        lidas.Select(campo => campo.Key).Should().Equal("beneficiarios", "categorias", "finais_sem_cartao");
        lidas["beneficiarios"]!.GetValue<int>().Should().Be(beneficiarios.Count);
        lidas["categorias"]!.GetValue<int>().Should().Be(categorias.Count);
        lidas["finais_sem_cartao"]!.AsArray().Select(final => final!.GetValue<string>()).Should().Equal((string)semCartao);
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_FalhaComPendenciasDoExtrato_Deve_LevarSoOsBeneficiarios(IReadOnlyList<Alias> beneficiarios)
    {
        JsonObject json = ImportacaoParaModelo
            .Falha(ErrosParaModelo.Importacao.BeneficiariosNaoResolvidos, PendenciasImportacaoParaModelo.DoExtrato(beneficiarios))
            .Serializar();

        JsonObject pendencias = json["pendencias"]!.AsObject();
        pendencias["beneficiarios"]!.GetValue<int>().Should().Be(beneficiarios.Count);
        pendencias["categorias"]!.GetValue<int>().Should().Be(0);
        pendencias["finais_sem_cartao"]!.AsArray().Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Resumo_Quando_ExtratoComContaExistente_Deve_DescreverOArquivoEAsContagens(ExtratoImportado importado)
    {
        ExtratoImportado contaExistente = importado with { ContaNova = false };

        string? resumo = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, contaExistente).Resumo;

        resumo.Should().Be(
            $"Importa o extrato c6-conta-csv da conta final {importado.SufixoConta} do {importado.Banco.Name}, de {Data(importado.Periodo.Inicio)} a {Data(importado.Periodo.Fim)}: "
            + $"{importado.Lidas} lidas, {importado.Novas} novas e {importado.JaExistentes} já existentes.");
    }

    [Theory, DomainAutoData]
    public void Resumo_Quando_ExtratoComContaNova_Deve_DizerQueCriaAConta(ExtratoImportado importado)
    {
        ExtratoImportado contaNova = importado with { ContaNova = true, TipoConta = TipoConta.ContaCorrente };

        string? resumo = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, contaNova).Resumo;

        resumo.Should().EndWith($"; cria a conta corrente final {importado.SufixoConta} do {importado.Banco.Name}.");
    }

    [Theory, DomainAutoData]
    public void Resumo_Quando_Fatura_Deve_DescreverCadaCartaoSemOsIds(Fatura fatura, FaturaImportada importada)
    {
        string? resumo = ImportacaoParaModelo.DaFatura(LayoutArquivoImportacao.C6CartaoCsv, fatura, importada).Resumo;

        FaturaImportada.CartaoImportado cartao = importada.Cartoes[0];
        resumo.Should()
            .StartWith($"Importa a fatura c6-cartao-csv do {importada.Banco.Name}, de ")
            .And.Contain($"cartão final {cartao.Sufixo}, {cartao.Lidas} lidas, {cartao.Novas} novas e {cartao.JaExistentes} já existentes, ")
            .And.NotContain(cartao.IdImportacao.Value.ToString())
            .And.NotContain(cartao.IdCartao.Value.ToString());
    }

    [Theory, DomainAutoData]
    public void ConfereResumo_Quando_ResumoDaPrevia_Deve_RetornarVerdadeiro(ExtratoImportado importado)
    {
        ImportacaoParaModelo importacao = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, importado);

        importacao.ConfereResumo(importacao.Resumo!).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void ConfereResumo_Quando_OutroFinalDeConta_Deve_RetornarFalso(ExtratoImportado importado, SufixoConta outroFinal)
    {
        string resumo = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.MercadoPagoContaCsv, importado).Resumo!;
        ImportacaoParaModelo outraConta = ImportacaoParaModelo.DoExtrato(
            LayoutArquivoImportacao.MercadoPagoContaCsv,
            importado with { SufixoConta = outroFinal });

        outraConta.ConfereResumo(resumo).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void ConfereResumo_Quando_OutraContagem_Deve_RetornarFalso(ExtratoImportado importado)
    {
        string resumo = ImportacaoParaModelo.DoExtrato(LayoutArquivoImportacao.C6ContaCsv, importado).Resumo!;
        ImportacaoParaModelo repetida = ImportacaoParaModelo.DoExtrato(
            LayoutArquivoImportacao.C6ContaCsv,
            importado with { Novas = 0, JaExistentes = importado.Lidas });

        repetida.ConfereResumo(resumo).Should().BeFalse();
    }

    private static (DateOnly Inicio, DateOnly Fim) Periodo(JsonObject json, string campo)
    {
        JsonObject periodo = json[campo]!.AsObject();
        DateOnly inicio = DateOnly.Parse(periodo.Texto("inicio"), CultureInfo.InvariantCulture);
        DateOnly fim = DateOnly.Parse(periodo.Texto("fim"), CultureInfo.InvariantCulture);
        return (inicio, fim);
    }

    private static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
