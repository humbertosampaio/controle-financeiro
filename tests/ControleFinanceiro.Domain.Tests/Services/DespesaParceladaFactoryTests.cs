using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class DespesaParceladaFactoryTests
{
    private const string DescricaoCompra = "LOJA DE EXEMPLO";
    private const string CategoriaCompra = "Eletrônicos";
    private const string TextoQueNormalizaParaVazio = "́";
    private static readonly DateOnly _dataCompra = new(2026, 03, 15);

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CompraSemDespesa_Deve_CriarADespesaDoCartao([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(idUsuario, [compra], TipoOrigem.Cartao);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas.Dos(idUsuario, [compra]);

        IReadOnlyList<DespesaParcelada> result = DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            beneficiarios,
            categorias,
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        DespesaParcelada despesa = result.Should().ContainSingle().Subject;
        MesReferencia mesPrimeiraParcela = VencimentoFaturaCartao.MesVencimento(cartao, _dataCompra);
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(beneficiarios.Single().Id);
        despesa.IdCategoria.Should().Be(categorias.Single().Id);
        despesa.IdCartao.Should().Be(cartao.Id);
        despesa.Descricao.Should().Be((TextoObrigatorio)DescricaoCompra);
        despesa.DataOcorrencia.Should().Be(_dataCompra);
        despesa.Parcelas.Should().HaveCount(10);
        despesa.Parcelas[0].MesReferencia.Should().Be(mesPrimeiraParcela);
        despesa.Parcelas[0].Pagamento.DataProjetada.Should().Be(mesPrimeiraParcela.NoDia(cartao.DiaVencimento));
        despesa.Parcelas.Should().OnlyContain(p => p.Pagamento.Valor == (ReaisPositivo)99.90m);
        despesa.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(2);
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CompraAVista_Deve_NaoCriar([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            parceladas: [],
            Compra(compra.Identidade, numero: null));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineCompraAutoData(99.90)]
    [InlineCompraAutoData(0)]
    public void CriarDaFatura_Quando_NaoEhSaida_Deve_NaoCriar(decimal valor, [Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            parceladas: [],
            Compra(compra.Identidade, valor: valor));

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_DescricaoNormalizaParaVazio_Deve_NaoCriar([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            parceladas: [],
            Compra(compra.Identidade, descricao: TextoQueNormalizaParaVazio));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineCompraAutoData(null)]
    [InlineCompraAutoData(TextoQueNormalizaParaVazio)]
    public void CriarDaFatura_Quando_SemCategoriaDoBanco_Deve_CriarSemCategoria(string? categoriaBanco, [Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            parceladas: [],
            Compra(compra.Identidade, categoria: categoriaBanco));

        result.Should().ContainSingle().Which.IdCategoria.Should().BeNull();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CompraJaTemDespesa_Deve_NaoCriarNemAlterar([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], compra).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            [existente],
            Compra(compra.Identidade, numero: 4, valor: -120m));

        result.Should().BeEmpty();
        existente.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(2);
        existente.Parcelas.Should().OnlyContain(p => p.Pagamento.Valor == (ReaisPositivo)99.90m);
    }

    [Theory]
    [InlineCompraAutoData("loja de exemplo")]
    [InlineCompraAutoData("LÓJA DE EXEMPLO")]
    [InlineCompraAutoData("  LOJA   DE  EXEMPLO ")]
    public void CriarDaFatura_Quando_DescricaoDifereNaForma_Deve_ReconhecerAMesmaCompra(
        string descricao,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        Lancamento compra)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], compra).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            [existente],
            Compra(compra.Identidade, descricao: descricao));

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_OutroTotal_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], compra).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            [existente],
            Compra(compra.Identidade, total: 12));

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_OutraData_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], compra).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            [existente],
            Compra(compra.Identidade, data: _dataCompra.AddDays(1)));

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_OutroCartao_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao, Cartao outroCartao, Lancamento compra)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, outroCartao, parceladas: [], compra).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], compra);

        result.Should().ContainSingle();
    }

    [Theory]
    [InlineCompraAutoData(0, 2)]
    [InlineCompraAutoData(1, 1)]
    [InlineCompraAutoData(2, 0)]
    public void CriarDaFatura_Quando_ComprasGemeas_Deve_CriarSoAsQueFaltam(
        int quantidadeExistentes,
        int quantidadeEsperada,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        Lancamento compra)
    {
        DespesaParcelada[] existentes = CriarDaFatura(idUsuario, cartao, parceladas: [], compra, compra)
            .Take(quantidadeExistentes)
            .ToArray();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, existentes, compra, compra);

        result.Should().HaveCount(quantidadeEsperada);
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_EmpateEntreNumeros_Deve_UsarOMenor([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(
            idUsuario,
            cartao,
            parceladas: [],
            Compra(compra.Identidade, numero: 4),
            Compra(compra.Identidade, numero: 3));

        result.Should().ContainSingle().Which.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(2);
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        Action act = () => CriarDaFatura(idUsuario, cartao, parceladas: [], compra);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartao");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_BeneficiarioDeOutroUsuario_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IdUsuario outroUsuario = IdUsuario.FromNewVersion7Guid();
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(outroUsuario, [compra], TipoOrigem.Cartao),
            CategoriasResolvidas.Dos(idUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CategoriaDeOutroUsuario_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        IdUsuario outroUsuario = IdUsuario.FromNewVersion7Guid();
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(idUsuario, [compra], TipoOrigem.Cartao),
            CategoriasResolvidas.Dos(outroUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    // A despesa vem antes do [Frozen]: só o que é pedido depois dele sai do usuário congelado.
    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_ParceladaDeOutroUsuario_Deve_LancarExcecao(
        DespesaParcelada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        Lancamento compra)
    {
        Action act = () => CriarDaFatura(idUsuario, cartao, [deOutroUsuario], compra);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("parceladasDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_BeneficiarioNaoEncontrado_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            beneficiariosDoUsuario: [],
            CategoriasResolvidas.Dos(idUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CategoriaNaoEncontrada_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao, Lancamento compra)
    {
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(idUsuario, [compra], TipoOrigem.Cartao),
            categoriasDoUsuario: [],
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    private static IReadOnlyList<DespesaParcelada> CriarDaFatura(
        IdUsuario idUsuario,
        Cartao cartao,
        IReadOnlyCollection<DespesaParcelada> parceladas,
        params Lancamento[] lancamentos)
    {
        return DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(idUsuario, lancamentos, TipoOrigem.Cartao),
            CategoriasResolvidas.Dos(idUsuario, lancamentos),
            cartao,
            parceladas,
            lancamentos);
    }

    /// <param name="numero">Nulo na compra à vista.</param>
    private static Lancamento Compra(
        IdentidadeLancamento identidade,
        int? numero = 3,
        int total = 10,
        decimal valor = -99.90m,
        DateOnly? data = null,
        string descricao = DescricaoCompra,
        string? categoria = CategoriaCompra)
    {
        return Lancamento.Criar(
            data ?? _dataCompra,
            hora: null,
            valor,
            (TextoObrigatorio)descricao,
            contraparte: null,
            numero is int n ? (Parcela)(n, total) : null,
            categoria,
            identidade);
    }

    /// <summary>Entrega a compra parcelada padrão no parâmetro <see cref="Lancamento"/> do teste.</summary>
    private sealed class CompraAutoDataAttribute : DomainAutoDataAttribute
    {
        public CompraAutoDataAttribute()
            : base(fixture => fixture.Register((IdentidadeLancamento identidade) => Compra(identidade)))
        {
        }
    }

    private sealed class InlineCompraAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineCompraAutoDataAttribute(params object?[]? values)
            : base(new CompraAutoDataAttribute(), values)
        {
        }
    }
}
