using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Importacoes.Eventos;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class ImportacaoArquivoFactoryTests
{
    [Theory, DomainAutoData]
    public void Registrar_Quando_Conta_Deve_GuardarOsDadosDaImportacaoComOUsuarioEAConta(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.IdUsuario.Should().Be(conta.IdUsuario);
        result.IdConta.Should().Be(conta.Id);
        result.Layout.Should().Be(layout);
        result.Periodo.Should().Be(periodo);
        result.Lidas.Should().Be(lidas);
        result.ImportadaEm.Should().Be(importadaEm);
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_Cartao_Deve_GuardarOUsuarioOCartaoEOVencimentoDaFatura(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        VencimentoFatura vencimento,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoFatura result = ImportacaoArquivoFactory
            .Registrar(cartao, cartoesUsuario: [cartao], importacoesFaturaMes: [], layout, periodo, vencimento, lidas, importadaEm)
            .Value;

        result.IdUsuario.Should().Be(cartao.IdUsuario);
        result.IdCartao.Should().Be(cartao.Id);
        result.Vencimento.Should().Be(vencimento);
        result.MesVencimento.Should().Be(vencimento.Mes);
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_Cartao_Deve_RegistrarFaturaCartaoImportadaEvent(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        VencimentoFatura vencimento,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoFatura result = ImportacaoArquivoFactory
            .Registrar(cartao, cartoesUsuario: [cartao], importacoesFaturaMes: [], layout, periodo, vencimento, lidas, importadaEm)
            .Value;

        result.Eventos.Should().Equal(new FaturaCartaoImportadaEvent(cartao.IdUsuario, result.Id, cartao.Id));
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_Conta_Deve_FicarSemEventos(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.Eventos.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_CartaoComLidasNegativas_Deve_LancarExcecao(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        VencimentoFatura vencimento,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoArquivoFactory.Registrar(
            cartao,
            cartoesUsuario: [cartao],
            importacoesFaturaMes: [],
            layout,
            periodo,
            vencimento,
            lidas: -1,
            importadaEm);

        registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas");
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_LidasNegativas_Deve_LancarExcecao(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas: -1, importadaEm);

        registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas");
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void Registrar_Quando_FaturaJaImportadaComOutraDataNoMesmoMes_Deve_Falhar(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura importada,
        DiaVencimentoCartao outroDia)
    {
        Result<ImportacaoFatura, Erro> result = ImportacaoArquivoFactory.Registrar(
            cartao,
            cartoesUsuario: [cartao],
            [importada],
            importada.Layout,
            importada.Periodo,
            VencimentoFatura.Projetado(mes, outroDia),
            importada.Lidas,
            importada.ImportadaEm);

        result.Should().Fail().Which.Should().Be(ErroFaturaJaImportadaComOutroVencimento.Instancia);
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_MesmoCartaoComOutraDataNoMesmoMes_Deve_Falhar(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura importada,
        DiaVencimentoCartao outroDia)
    {
        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            VencimentoFatura.Projetado(mes, outroDia),
            cartoesFatura: [cartao],
            cartoesUsuario: [cartao],
            [importada]);

        result.Should().Fail().Which.Should().Be(ErroFaturaJaImportadaComOutroVencimento.Instancia);
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_OutroCartaoMesmaFaturaComOutraDataNoMesmoMes_Deve_Falhar(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura importada,
        SufixoCartao outroSufixo,
        DiaVencimentoCartao outroDia)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);

        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            VencimentoFatura.Projetado(mes, outroDia),
            cartoesFatura: [outroFinal],
            cartoesUsuario: [cartao, outroFinal],
            [importada]);

        result.Should().Fail().Which.Should().Be(ErroFaturaJaImportadaComOutroVencimento.Instancia);
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_CartaoOutraFatura_Deve_Aceitar(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura importada,
        DiaVencimentoCartao outroDia)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();

        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartaoOutroBanco.Banco,
            VencimentoFatura.Projetado(mes, outroDia),
            cartoesFatura: [cartaoOutroBanco],
            cartoesUsuario: [cartao, cartaoOutroBanco],
            [importada]);

        result.Should().Succeed();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_MesmaData_Deve_Aceitar([Frozen] Cartao cartao, ImportacaoFatura importada)
    {
        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            importada.Vencimento,
            cartoesFatura: [cartao],
            cartoesUsuario: [cartao],
            [importada]);

        result.Should().Succeed();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_OutroMes_Deve_Aceitar(
        [Frozen] Cartao cartao,
        ImportacaoFatura importada,
        MesReferencia outroMes,
        DiaVencimentoCartao dia)
    {
        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            VencimentoFatura.Projetado(outroMes, dia),
            cartoesFatura: [cartao],
            cartoesUsuario: [cartao],
            [importada]);

        result.Should().Succeed();
    }

    [Theory, DomainAutoData]
    public void ConferirVencimentoFatura_Quando_SemImportacoes_Deve_Aceitar(Cartao cartao, VencimentoFatura vencimento)
    {
        UnitResult<Erro> result = ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            vencimento,
            cartoesFatura: [cartao],
            cartoesUsuario: [cartao],
            importacoesFaturaMes: []);

        result.Should().Succeed();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void ConferirVencimentoFatura_Quando_ImportacaoOutroUsuario_Deve_LancarExcecao(
        Cartao cartao,
        ImportacaoFatura importacaoOutroUsuario,
        VencimentoFatura vencimento)
    {
        Action conferir = () => ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            vencimento,
            cartoesFatura: [cartao],
            cartoesUsuario: [cartao],
            [importacaoOutroUsuario]);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("importacoesFaturaMes");
    }

    [Theory, DomainAutoData]
    public void ConferirVencimentoFatura_Quando_CartaoUsuarioDeOutroUsuario_Deve_LancarExcecao(
        Cartao cartao,
        Cartao cartaoOutroUsuario,
        VencimentoFatura vencimento)
    {
        Action conferir = () => ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            vencimento,
            cartoesFatura: [cartao],
            [cartao, cartaoOutroUsuario],
            importacoesFaturaMes: []);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesUsuario");
    }

    [Theory, DomainAutoData]
    public void ConferirVencimentoFatura_Quando_CartaoFaturaDeOutroUsuario_Deve_LancarExcecao(
        Cartao cartao,
        Cartao cartaoOutroUsuario,
        VencimentoFatura vencimento)
    {
        Action conferir = () => ImportacaoArquivoFactory.ConferirVencimentoFatura(
            cartao.IdUsuario,
            cartao.Banco,
            vencimento,
            [cartaoOutroUsuario],
            cartoesUsuario: [cartao],
            importacoesFaturaMes: []);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesFatura");
    }

    [Theory, DomainAutoData]
    public void Registrar_Deve_GerarIdVersao7(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.Id.Value.Version.Should().Be(7);
    }
}
