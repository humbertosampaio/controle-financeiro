using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class DespesaFactoryTests
{
    [Theory, DomainAutoData]
    public void CriarComPagamentoAdiado_Quando_FormaPagamentoCredito_Deve_RetornarDespesaAdiadaDoUsuarioDaRecorrente(
        [Frozen] Cartao cartao,
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 02, diaCompra.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = DespesaFactory.CriarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        DespesaAdiada despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>().Subject;
        despesa.IdUsuario.Should().Be(despesaRecorrente.IdUsuario);
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void CriarComPagamentoAdiado_Quando_FormaPagamentoCredito_Deve_UsarVencimentoDoCartao(
        [Frozen] DiaVencimentoCartao diaVencimento,
        [Frozen] Cartao cartao,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset vencimentoEsperado = new DateOnly(2026, 03, diaVencimento.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = DespesaFactory.CriarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>()
            .Which.Pagamento.DataProjetada.Should().Be(vencimentoEsperado);
    }

    [Theory, DomainAutoData]
    public void CriarComPagamentoAdiado_Quando_CreditoComCartaoDiferenteDoDaRecorrente_Deve_LancarExcecao(
        MesReferencia mesReferencia,
        Cartao outroCartao,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => DespesaFactory.CriarComPagamentoAdiado(despesaRecorrente, outroCartao, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void CriarComPagamentoImediato_Quando_RecorrenteNoCredito_Deve_LancarExcecao(
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => DespesaFactory.CriarComPagamentoImediato(despesaRecorrente, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory]
    [InlineDomainAutoData(1)] // Pix
    [InlineDomainAutoData(3)] // Boleto
    public void CriarComPagamentoAdiado_Quando_CartaoEmRecorrenteSemCartao_Deve_LancarExcecao(
        int valorFormaPagamento,
        MesReferencia mesReferencia,
        Cartao cartao,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.FromValue(valorFormaPagamento));
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => DespesaFactory.CriarComPagamentoAdiado(despesaRecorrente, cartao, mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void CriarComPagamentoImediato_Quando_FormaPagamentoPix_Deve_RetornarDespesaSimplesDoUsuarioDaRecorrente(
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompra.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Pix);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = DespesaFactory.CriarComPagamentoImediato(despesaRecorrente, mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.IdUsuario.Should().Be(despesaRecorrente.IdUsuario);
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void CriarComPagamentoImediato_Quando_FormaPagamentoBoleto_Deve_RetornarDespesaSimples(
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompra.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Boleto);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = DespesaFactory.CriarComPagamentoImediato(despesaRecorrente, mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }
}
