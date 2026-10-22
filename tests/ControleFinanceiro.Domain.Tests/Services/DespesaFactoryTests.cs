using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
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
    public void Criar_Quando_FormaPagamentoCredito_Deve_RetornarDespesaAdiada(
        Usuario usuario,
        Cartao cartao,
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 02, diaCompra.Value).ToLocalDateTimeOffset();
        usuario.AdicionarCartao(cartao).Should().Succeed();
        fixture.Inject(FormaPagamento.Credito);
        fixture.Inject(cartao.Id);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>()).Should().Succeed();

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaAdiada despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoCredito_Deve_UsarVencimentoDoCartaoDaDespesa(
        Usuario usuario,
        SufixoCartao sufixo,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset vencimentoEsperado = new DateOnly(2026, 03, 10).ToLocalDateTimeOffset();
        Cartao cartaoDaDespesa = Cartao.Criar(Banco.C6, sufixo, (DiaVencimentoCartao)10);
        Cartao outroCartao = Cartao.Criar(Banco.MercadoPago, sufixo, (DiaVencimentoCartao)20);
        usuario.AdicionarCartao(outroCartao).Should().Succeed();
        usuario.AdicionarCartao(cartaoDaDespesa).Should().Succeed();
        fixture.Inject(FormaPagamento.Credito);
        fixture.Inject(cartaoDaDespesa.Id);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>()).Should().Succeed();

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        result.Should().Succeed()
            .Which.Should().BeOfType<DespesaAdiada>()
            .Which.Pagamento.DataProjetada.Should().Be(vencimentoEsperado);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_CartaoDaDespesaNaoPertenceAoUsuario_Deve_RetornarFalha(
        Usuario usuario,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();

        Result<Despesa> result = DespesaFactory.Criar(usuario, despesaRecorrente, mesReferencia);

        result.Should().FailWith("O cartão da despesa recorrente não pertence ao usuário.");
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoPix_Deve_RetornarDespesaSimples(
        Usuario usuario,
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompra.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Pix);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>()).Should().Succeed();

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FormaPagamentoBoleto_Deve_RetornarDespesaSimples(
        Usuario usuario,
        [Frozen] DiaCompraRecorrente diaCompra,
        Fixture fixture)
    {
        MesReferencia mesReferencia = MesReferencia.Criar(03, 2026).Value;
        DateTimeOffset dataCompraEsperada = new DateOnly(2026, 03, diaCompra.Value).ToLocalDateTimeOffset();
        fixture.Inject(FormaPagamento.Boleto);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>()).Should().Succeed();

        Result<Despesa> result = DespesaFactory.Criar(usuario, usuario.DespesasRecorrentes[0], mesReferencia);

        DespesaSimples despesa = result.Should().Succeed()
            .Which.Should().BeOfType<DespesaSimples>().Subject;
        despesa.DataCompra.Should().Be(dataCompraEsperada);
    }
}
