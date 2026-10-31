using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Despesas;

public sealed class DespesaParceladaTests
{
    private static readonly DateOnly _dataOcorrencia = new(2026, 01, 15);
    private static readonly MesReferencia _mesPrimeiraParcela = (MesReferencia)(02, 2026);
    private static readonly DiaVencimentoCartao _diaVencimento = (DiaVencimentoCartao)10;

    /// <summary>O que a compra precisa e o teste não verifica.</summary>
    public sealed record Compra(IdUsuario IdUsuario, IdBeneficiario IdBeneficiario, TextoObrigatorio Descricao, ReaisPositivo Valor, IdCartao IdCartao)
    {
        public DespesaParcelada Parcelar(int numero, int total)
        {
            return DespesaParcelada.Criar(
                IdUsuario,
                IdBeneficiario,
                Descricao,
                idCategoria: null,
                _dataOcorrencia,
                Valor,
                IdCartao,
                _mesPrimeiraParcela,
                _diaVencimento,
                (Parcela)(numero, total));
        }
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ParametrosValidos_Deve_PreencherADespesaNoCredito(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento)
    {
        DespesaParcelada result = DespesaParcelada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            _dataOcorrencia,
            valor,
            idCartao,
            _mesPrimeiraParcela,
            diaVencimento,
            (Parcela)(1, 2));

        result.IdUsuario.Should().Be(idUsuario);
        result.IdBeneficiario.Should().Be(idBeneficiario);
        result.Descricao.Should().Be(descricao);
        result.IdCategoria.Should().Be(idCategoria);
        result.DataOcorrencia.Should().Be(_dataOcorrencia);
        result.FormaPagamento.Should().Be(FormaPagamento.Credito);
        result.IdCartao.Should().Be(idCartao);
        result.IdDespesaRecorrente.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_NumerarTodasAsParcelasDaCompra(Compra compra)
    {
        DespesaParcelada result = compra.Parcelar(numero: 3, total: 10);

        result.Parcelas.Select(p => p.NumeroParcela).Should().Equal(Enumerable.Range(1, 10));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_ParcelasViramOAno_Deve_ManterOsMesesConsecutivos(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        IdCartao idCartao)
    {
        DespesaParcelada result = DespesaParcelada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            new DateOnly(2025, 10, 20),
            valor,
            idCartao,
            (MesReferencia)(11, 2025),
            _diaVencimento,
            (Parcela)(1, 3));

        result.Parcelas.Select(p => p.MesReferencia).Should().Equal((MesReferencia)(11, 2025), (MesReferencia)(12, 2025), (MesReferencia)(01, 2026));
        result.Parcelas.Select(p => p.Pagamento.DataProjetada).Should().Equal(new DateOnly(2025, 11, 10), new DateOnly(2025, 12, 10), new DateOnly(2026, 01, 10));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_MarcarComoPagasAsParcelasAnterioresAInformada(Compra compra)
    {
        DespesaParcelada result = compra.Parcelar(numero: 3, total: 10);

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
    public void Criar_Deve_DarOMesmoValorATodasAsParcelas(Compra compra)
    {
        DespesaParcelada result = compra.Parcelar(numero: 3, total: 10);

        result.Parcelas.Should().OnlyContain(p => p.Pagamento.Valor == compra.Valor);
    }

    [Theory]
    [InlineDomainAutoData(1, 0)]
    [InlineDomainAutoData(2, 1)]
    public void Criar_Quando_CompraEmDuasParcelas_Deve_PagarSoAsAnteriores(int numero, int pagasEsperadas, Compra compra)
    {
        DespesaParcelada result = compra.Parcelar(numero, total: 2);

        result.Parcelas.Should().HaveCount(2);
        result.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(pagasEsperadas);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_PrimeiraParcelaNoMesDaOcorrencia_Deve_Aceitar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento)
    {
        DespesaParcelada result = DespesaParcelada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            new DateOnly(2026, 03, 05),
            valor,
            idCartao,
            (MesReferencia)(03, 2026),
            diaVencimento,
            (Parcela)(1, 2));

        result.Parcelas[0].MesReferencia.Should().Be((MesReferencia)(03, 2026));
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_PrimeiraParcelaAntesDoMesDaOcorrencia_Deve_LancarExcecao(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento)
    {
        Action act = () => DespesaParcelada.Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria: null,
            new DateOnly(2026, 03, 05),
            valor,
            idCartao,
            (MesReferencia)(02, 2026),
            diaVencimento,
            (Parcela)(1, 2));

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("mesPrimeiraParcela");
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GerarIdVersao7ParaADespesaEParaCadaParcela(Compra compra)
    {
        DespesaParcelada result = compra.Parcelar(numero: 1, total: 2);

        result.Id.Value.Version.Should().Be(7);
        result.Parcelas.Should().OnlyContain(p => p.Id.Value.Version == 7);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_HaParcelaNoMes_Deve_RetornarAParcela(Compra compra)
    {
        DespesaParcelada despesa = compra.Parcelar(numero: 1, total: 3);

        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes((MesReferencia)(03, 2026));

        result.Should().HaveSomeValue().Which.NumeroParcela.Should().Be(2);
    }

    [Theory, DomainAutoData]
    public void ParcelaDoMes_Quando_NaoHaParcelaNoMes_Deve_RetornarVazio(Compra compra)
    {
        DespesaParcelada despesa = compra.Parcelar(numero: 1, total: 3);

        Maybe<ParcelaDespesa> result = despesa.ParcelaDoMes((MesReferencia)(05, 2026));

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void AlterarCategoria_Deve_TrocarACategoria(Compra compra, IdCategoriaDespesa idCategoria)
    {
        DespesaParcelada despesa = compra.Parcelar(numero: 1, total: 2);

        despesa.AlterarCategoria(idCategoria);

        despesa.IdCategoria.Should().Be(idCategoria);
    }
}
