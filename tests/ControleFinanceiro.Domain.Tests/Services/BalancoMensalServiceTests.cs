using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.Extensions;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class BalancoMensalServiceTests
{
    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_SemDespesas_Deve_TerSaldoIgualARendaLiquida(Usuario usuario, MesReferencia mesReferencia)
    {
        BalancoMensal result = BalancoMensalService.Calcular(usuario, despesasDoMes: [], mesReferencia);

        result.Mes.Should().Be(mesReferencia);
        result.RendaLiquida.Should().Be(usuario.RendaLiquida);
        result.TotalPago.Should().Be(ReaisPositivo.Zero);
        result.TotalPendente.Should().Be(ReaisPositivo.Zero);
        result.SaldoPrevisto.Should().Be(usuario.RendaLiquida);
    }

    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_SoDespesasPagas_Deve_SomarOsValoresNoPago(Usuario usuario, Fixture fixture)
    {
        fixture.Inject((Parcela)(3, 10));
        DespesaParcelada[] despesas = fixture.CreateMany<DespesaParcelada>(count: 2).ToArray();
        ParcelaDespesa primeira = despesas[0].Parcelas[0];

        BalancoMensal result = BalancoMensalService.Calcular(usuario, despesas, primeira.MesReferencia);

        result.TotalPago.Should().Be(despesas.Sum(d => d.Parcelas[0].ValorVigente));
        result.TotalPendente.Should().Be(ReaisPositivo.Zero);
    }

    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_SoDespesasPendentes_Deve_SomarOsValoresNoPendente(Usuario usuario, DespesaSimples[] despesas)
    {
        BalancoMensal result = BalancoMensalService.Calcular(usuario, despesas, despesas[0].MesReferencia);

        result.TotalPago.Should().Be(ReaisPositivo.Zero);
        result.TotalPendente.Should().Be(despesas.Sum(d => d.Pagamento.ValorProjetado));
    }

    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_ParceladaPagaEPendenteNoMes_Deve_DescontarAsDuasDaRendaLiquida(Usuario usuario, Fixture fixture)
    {
        fixture.Inject((Parcela)(2, 10));
        DespesaParcelada paga = fixture.Create<DespesaParcelada>();
        fixture.Inject((Parcela)(1, 10));
        DespesaParcelada pendente = fixture.Create<DespesaParcelada>();
        ParcelaDespesa parcelaPaga = paga.Parcelas[0];
        ParcelaDespesa parcelaPendente = pendente.Parcelas[0];

        BalancoMensal result = BalancoMensalService.Calcular(usuario, [paga, pendente], parcelaPaga.MesReferencia);

        result.TotalPago.Should().Be(parcelaPaga.ValorVigente);
        result.TotalPendente.Should().Be(parcelaPendente.ValorVigente);
        result.SaldoPrevisto.Should().Be(usuario.RendaLiquida - parcelaPaga.ValorProjetado - parcelaPendente.ValorProjetado);
    }

    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_AdiadaCobradaENaoPaga_Deve_SomarOValorCobradoNoPendente(
        Usuario usuario,
        Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        VinculoTransacaoServiceTests.SaidaDeTeste saida)
    {
        Transacao compra = transacoes.DoCartao(cartao, saida.AVista());
        VinculoTransacaoService.Vincular(compra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();

        BalancoMensal result = BalancoMensalService.Calcular(usuario, [despesa], despesa.MesReferencia);

        result.TotalPago.Should().Be(ReaisPositivo.Zero);
        result.TotalPendente.Should().Be(saida.ValorPago);
    }

    // A despesa vale um real a mais que a renda líquida, em módulo: o saldo fica negativo qualquer que seja a renda gerada.
    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_DespesasPassamDaRendaLiquida_Deve_TerSaldoNegativo(Usuario usuario, Fixture fixture)
    {
        fixture.Inject((ReaisPositivo)(decimal.Abs(usuario.RendaLiquida) + 1m));
        DespesaSimples despesa = fixture.Create<DespesaSimples>();

        BalancoMensal result = BalancoMensalService.Calcular(usuario, [despesa], despesa.MesReferencia);

        result.SaldoPrevisto.Value.Should().BeNegative();
    }

    [Theory, DomainAutoData]
    public void Calcular_Quando_DespesaDeOutroUsuario_Deve_LancarExcecao(Usuario usuario, DespesaSimples despesaDeOutroUsuario)
    {
        Action act = () => BalancoMensalService.Calcular(usuario, [despesaDeOutroUsuario], despesaDeOutroUsuario.MesReferencia);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasDoMes");
    }

    [Theory, DespesasDoUsuarioAutoData]
    public void Calcular_Quando_DespesaSemPagamentoNoMes_Deve_LancarExcecao(Usuario usuario, DespesaSimples despesa)
    {
        Action act = () => BalancoMensalService.Calcular(usuario, [despesa], despesa.MesReferencia.Posterior());

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesasDoMes");
    }

    /// <summary>
    /// As despesas saem do usuário pedido no teste e caem no mesmo mês: as simples na mesma data, e as parceladas na
    /// mesma compra do mesmo cartão.
    /// </summary>
    private sealed class DespesasDoUsuarioAutoDataAttribute : DomainAutoDataAttribute
    {
        public DespesasDoUsuarioAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Inject(fixture.Freeze<Usuario>().Id);
                fixture.Freeze<Cartao>();
                fixture.Freeze<DateOnly>();
            })
        {
        }
    }
}
