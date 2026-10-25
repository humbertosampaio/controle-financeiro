using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class GeracaoDespesasDoMesServiceTests
{
    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_DespesasRecorrentesSemCartao_Deve_RetornarUmaDespesaSimplesPorRecorrente(
        [Frozen] IdUsuario idUsuario,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Pix);
        List<DespesaRecorrente> recorrentes = fixture.CreateMany<DespesaRecorrente>().ToList();

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasDoMesService.GerarParaMes(recorrentes, cartoesDoUsuario: [], mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(recorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(idUsuario);
            despesa.Should().BeOfType<DespesaSimples>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_DespesasRecorrentesNoCredito_Deve_RetornarUmaDespesaAdiadaPorRecorrente(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        Cartao outroCartao,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        List<DespesaRecorrente> recorrentes = fixture.CreateMany<DespesaRecorrente>().ToList();

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasDoMesService.GerarParaMes(
            recorrentes, [outroCartao, cartao], mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(recorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(idUsuario);
            despesa.Should().BeOfType<DespesaAdiada>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_CartaoDaRecorrenteForaDaLista_Deve_LancarExcecao(
        Cartao outroCartao,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente recorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => GeracaoDespesasDoMesService.GerarParaMes([recorrente], [outroCartao], mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_SemDespesasRecorrentes_Deve_RetornarListaVazia(MesReferencia mesReferencia)
    {
        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasDoMesService.GerarParaMes(recorrentes: [], cartoesDoUsuario: [], mesReferencia);

        result.Should().Succeed()
            .Which.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoAdiado_Quando_FormaPagamentoCredito_Deve_RetornarDespesaAdiadaDoUsuarioDaRecorrente(
        [Frozen] Cartao cartao,
        [Frozen] DiaOcorrenciaRecorrente diaOcorrencia,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(mes: 03, ano: 2026).Value;
        DateTimeOffset dataOcorrenciaEsperada = new DateOnly(2026, 02, diaOcorrencia.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = GeracaoDespesasDoMesService.GerarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        DespesaAdiada despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>().Subject;
        despesa.IdUsuario.Should().Be(despesaRecorrente.IdUsuario);
        despesa.DataOcorrencia.Should().Be(dataOcorrenciaEsperada);
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoAdiado_Quando_FormaPagamentoCredito_Deve_UsarVencimentoDoCartao(
        [Frozen] DiaVencimentoCartao diaVencimento,
        [Frozen] Cartao cartao,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(mes: 03, ano: 2026).Value;
        DateTimeOffset vencimentoEsperado = new DateOnly(2026, 03, diaVencimento.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = GeracaoDespesasDoMesService.GerarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>()
            .Which.Pagamento.DataProjetada.Should().Be(vencimentoEsperado);
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoAdiado_Quando_CreditoComCartaoDiferenteDoDaRecorrente_Deve_LancarExcecao(
        MesReferencia mesReferencia,
        Cartao outroCartao,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => GeracaoDespesasDoMesService.GerarComPagamentoAdiado(despesaRecorrente, outroCartao, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoImediato_Quando_RecorrenteNoCredito_Deve_LancarExcecao(
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => GeracaoDespesasDoMesService.GerarComPagamentoImediato(despesaRecorrente, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void GerarComPagamentoAdiado_Quando_CartaoEmRecorrenteSemCartao_Deve_LancarExcecao(
        int valorFormaPagamento,
        MesReferencia mesReferencia,
        Cartao cartao,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.FromValue(valorFormaPagamento));
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => GeracaoDespesasDoMesService.GerarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoImediato_Quando_FormaPagamentoPix_Deve_RetornarDespesaSimplesDoUsuarioDaRecorrente(
        [Frozen] DiaOcorrenciaRecorrente diaOcorrencia,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(mes: 03, ano: 2026).Value;
        DateTimeOffset dataOcorrenciaEsperada = new DateOnly(2026, 03, diaOcorrencia.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Pix);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = GeracaoDespesasDoMesService.GerarComPagamentoImediato(despesaRecorrente, mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.IdUsuario.Should().Be(despesaRecorrente.IdUsuario);
        despesa.DataOcorrencia.Should().Be(dataOcorrenciaEsperada);
    }

    [Theory, DomainAutoData]
    public void GerarComPagamentoImediato_Quando_FormaPagamentoBoleto_Deve_RetornarDespesaSimples(
        [Frozen] DiaOcorrenciaRecorrente diaOcorrencia,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(mes: 03, ano: 2026).Value;
        DateTimeOffset dataOcorrenciaEsperada = new DateOnly(2026, 03, diaOcorrencia.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Boleto);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = GeracaoDespesasDoMesService.GerarComPagamentoImediato(despesaRecorrente, mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataOcorrencia.Should().Be(dataOcorrenciaEsperada);
    }
}
