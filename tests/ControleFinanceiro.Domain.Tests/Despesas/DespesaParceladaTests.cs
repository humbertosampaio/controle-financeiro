using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Tests.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

/// <summary>O <c>Criar</c> é interno: a despesa nasce pela <see cref="DespesaParceladaFactory"/>, como na aplicação.</summary>
public sealed class DespesaParceladaTests
{
    private static readonly DateOnly _dataOcorrencia = new(2026, 01, 15);

    [Theory, DomainAutoData]
    public void Criar_Deve_PreencherADespesaNoCredito([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 1, total: 2);

        result.IdUsuario.Should().Be(idUsuario);
        result.DataOcorrencia.Should().Be(_dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(cartao.Id);
        result.IdDespesaRecorrente.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_NumerarTodasAsParcelasDaCompra([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Select(p => p.NumeroParcela).Should().Equal(Enumerable.Range(1, 10));
    }

    // Doze parcelas viram o ano qualquer que seja o mês da primeira.
    [Theory, DomainAutoData]
    public void Criar_Quando_ParcelasViramOAno_Deve_ManterOsMesesConsecutivosNoVencimentoDoCartao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 1, total: 12);

        MesReferencia mes = VencimentoFaturaCartao.MesVencimento(cartao, _dataOcorrencia);
        foreach (ParcelaDespesa parcela in result.Parcelas)
        {
            parcela.MesReferencia.Should().Be(mes);
            parcela.Pagamento.DataProjetada.Should().Be(mes.NoDia(cartao.DiaVencimento));
            mes = mes.Posterior();
        }
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_MarcarComoPagasAsParcelasAnterioresAInformada([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Take(2).Should().AllSatisfy(p =>
        {
            p.Pagamento.Status.Should().Be(StatusPagamento.Pago);
            p.Pagamento.DataEfetiva.Should().NotBeNull().And.Be(p.Pagamento.DataProjetada);
        });
        result.Parcelas.Skip(2).Should().AllSatisfy(p =>
        {
            p.Pagamento.Status.Should().Be(StatusPagamento.Pendente);
            p.Pagamento.DataEfetiva.Should().BeNull();
        });
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_DarOMesmoValorATodasAsParcelas([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero: 3, total: 10);

        result.Parcelas.Should().OnlyContain(p => p.Pagamento.Valor == (ReaisPositivo)99.90m);
    }

    [Theory]
    [InlineDomainAutoData(1, 0)]
    [InlineDomainAutoData(2, 1)]
    public void Criar_Quando_CompraEmDuasParcelas_Deve_PagarSoAsAnteriores(int numero, int pagasEsperadas, [Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada result = Parcelar(idUsuario, cartao, numero, total: 2);

        result.Parcelas.Should().HaveCount(2);
        result.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(pagasEsperadas);
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaADespesaEParaCadaParcela(DespesaParcelada despesa)
    {
        despesa.Id.Value.Version.Should().Be(7);
        despesa.Parcelas.Should().OnlyContain(p => p.Id.Value.Version == 7);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_HaParcelaNoMes_Deve_RetornarAParcela(DespesaParcelada despesa)
    {
        ParcelaDespesa ultima = despesa.Parcelas[^1];

        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes(ultima.MesReferencia);

        result.HasValue.Should().BeTrue();
        result.Value.Should().BeSameAs(ultima);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_NaoHaParcelaNoMes_Deve_RetornarVazio(DespesaParcelada despesa)
    {
        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes(despesa.Parcelas[^1].MesReferencia.Posterior());

        result.HasNoValue.Should().BeTrue();
    }

    private static DespesaParcelada Parcelar(IdUsuario idUsuario, Cartao cartao, int numero, int total)
    {
        Lancamento compra = Lancamento.Criar(
            _dataOcorrencia,
            hora: null,
            valor: -99.90m,
            (TextoObrigatorio)"LOJA DE EXEMPLO",
            contraparte: null,
            (Parcela)(numero, total),
            categoriaBanco: null,
            IdentidadeLancamento.Unica("1").Value);

        return DespesaParceladaFactory
            .CriarDaFatura(
                idUsuario,
                BeneficiariosResolvidos.Dos(idUsuario, [compra], TipoOrigem.Cartao),
                categoriasDoUsuario: [],
                cartao,
                parceladasDoUsuario: [],
                [compra])
            .Single();
    }
}
