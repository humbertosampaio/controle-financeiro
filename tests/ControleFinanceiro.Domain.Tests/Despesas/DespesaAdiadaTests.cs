using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;
using SaidaDeTeste = ControleFinanceiro.Domain.Tests.Services.VinculoTransacaoServiceTests.SaidaDeTeste;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class DespesaAdiadaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_VencimentoPosteriorADataOcorrencia_Deve_RetornarSucesso(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        IdCartao idCartao)
    {
        DateOnly dataOcorrencia = new(2026, 02, 05);
        MesReferencia mesVencimento = (MesReferencia)(04, 2026);
        VencimentoFatura vencimento = VencimentoFatura.Projetado(mesVencimento, (DiaVencimentoCartao)05);
        DateOnly dataProjetadaPagamentoEsperada = new(2026, 04, 05);

        DespesaAdiada result = DespesaAdiada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            valor,
            idCartao,
            vencimento,
            idDespesaRecorrente: null);

        result.IdUsuario.Should().Be(idUsuario);
        result.IdBeneficiario.Should().Be(idBeneficiario);
        result.Descricao.Should().Be(descricao);
        result.IdCategoria.Should().Be(idCategoria);
        result.DataOcorrencia.Should().Be(dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(idCartao);
        result.MesReferencia.Should().Be(mesVencimento);

        Pagamento pagamento = result.Pagamento;
        pagamento.DataProjetada.Should().Be(dataProjetadaPagamentoEsperada);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.ValorProjetado.Should().Be(valor);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_MesVencimentoAnteriorAoDaOcorrencia_Deve_LancarExcecao(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento)
    {
        Action act = () => DespesaAdiada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            new DateOnly(2026, 03, 05),
            valor,
            idCartao,
            VencimentoFatura.Projetado((MesReferencia)(02, 2026), diaVencimento),
            idDespesaRecorrente: null);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("vencimento");
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_PagaPelaSaida_Deve_SerVerdadeiro(
        [Frozen] Cartao cartao,
        DespesaAdiada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagar(sut, cartao, transacoes, compra, pagamento);

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.MesReferencia)).Should().BeTrue();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_CobradaEPendente_Deve_SerFalso(
        [Frozen] Cartao cartao,
        DespesaAdiada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra)
    {
        Cobrar(sut, cartao, transacoes.DoCartao(cartao, compra.AVista()));

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.MesReferencia)).Should().BeFalse();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_FaturaDeOutroMes_Deve_SerFalso(
        [Frozen] Cartao cartao,
        DespesaAdiada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagar(sut, cartao, transacoes, compra, pagamento);

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartao.Id, [cartao], sut.MesReferencia.Posterior())).Should().BeFalse();
    }

    [Theory, PagaPelaFaturaAutoData]
    public void EstaPagaPelaFatura_Quando_FaturaDeOutroCartao_Deve_SerFalso(
        [Frozen] Cartao cartao,
        DespesaAdiada sut,
        TransacoesDeTeste transacoes,
        SaidaDeTeste compra,
        SaidaDeTeste pagamento)
    {
        CobrarEPagar(sut, cartao, transacoes, compra, pagamento);
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();

        sut.EstaPagaPelaFatura(FaturaCartao.DoCartao(cartaoOutroBanco.Id, [cartaoOutroBanco], sut.MesReferencia)).Should().BeFalse();
    }

    private static void CobrarEPagar(DespesaAdiada despesa, Cartao cartao, TransacoesDeTeste transacoes, SaidaDeTeste compra, SaidaDeTeste pagamento)
    {
        Cobrar(despesa, cartao, transacoes.DoCartao(cartao, compra.AVista()));
        PagamentoFaturaService
            .Pagar(transacoes.DaConta(pagamento.AVista()), cartao, despesa.MesReferencia, [cartao], [despesa], despesasPagasPelaSaida: [])
            .Should().Succeed();
    }

    private static void Cobrar(DespesaAdiada despesa, Cartao cartao, Transacao compra)
    {
        VinculoTransacaoService.Vincular(compra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: [cartao]).Should().Succeed();
    }

    /// <summary>Tudo sai do mesmo usuário e na mesma data, para a compra cair no mês de vencimento da despesa.</summary>
    private sealed class PagaPelaFaturaAutoDataAttribute : DomainAutoDataAttribute
    {
        public PagaPelaFaturaAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Freeze<DateOnly>();
            })
        {
        }
    }
}
