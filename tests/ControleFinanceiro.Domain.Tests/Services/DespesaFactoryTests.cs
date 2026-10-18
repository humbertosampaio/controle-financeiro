using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class DespesaFactoryTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoCredito_Deve_RetornarDespesaAdiada(Usuario usuario, Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        const int diaCompraRecorrente = 9;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 02, diaCompraRecorrente).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Credito);
        fixture.Inject((DiaCompraRecorrente)diaCompraRecorrente);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>());

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaAdiada despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoPix_Deve_RetornarDespesaSimples(Usuario usuario, Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        const int diaCompraRecorrente = 9;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompraRecorrente).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Pix);
        fixture.Inject((DiaCompraRecorrente)diaCompraRecorrente);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>());

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoBoleto_Deve_RetornarDespesaSimples(Usuario usuario, Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        const int diaCompraRecorrente = 9;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompraRecorrente).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Boleto);
        fixture.Inject((DiaCompraRecorrente)diaCompraRecorrente);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>());

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }
}