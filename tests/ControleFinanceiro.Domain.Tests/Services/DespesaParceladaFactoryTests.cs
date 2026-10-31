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

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CompraSemDespesa_Deve_CriarADespesaDoCartao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        Lancamento compra = Compra();
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

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CompraAVista_Deve_NaoCriar([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(numero: null));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineDomainAutoData(99.90)]
    [InlineDomainAutoData(0)]
    public void CriarDaFatura_Quando_NaoEhSaida_Deve_NaoCriar(decimal valor, [Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(valor: valor));

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_DescricaoNormalizaParaVazio_Deve_NaoCriar([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(descricao: TextoQueNormalizaParaVazio));

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineDomainAutoData(null)]
    [InlineDomainAutoData(TextoQueNormalizaParaVazio)]
    public void CriarDaFatura_Quando_SemCategoriaDoBanco_Deve_CriarSemCategoria(string? categoriaBanco, [Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(categoria: categoriaBanco));

        result.Should().ContainSingle().Which.IdCategoria.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CompraJaTemDespesa_Deve_NaoCriarNemAlterar([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra()).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], Compra(numero: 4, valor: -120m));

        result.Should().BeEmpty();
        existente.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(2);
        existente.Parcelas.Should().OnlyContain(p => p.Pagamento.Valor == (ReaisPositivo)99.90m);
    }

    [Theory]
    [InlineDomainAutoData("loja de exemplo")]
    [InlineDomainAutoData("LÓJA DE EXEMPLO")]
    [InlineDomainAutoData("  LOJA   DE  EXEMPLO ")]
    public void CriarDaFatura_Quando_DescricaoDifereNaForma_Deve_ReconhecerAMesmaCompra(
        string descricao,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra()).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], Compra(descricao: descricao));

        result.Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_OutroTotal_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra()).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], Compra(total: 12));

        result.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_OutraData_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra()).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], Compra(data: _dataCompra.AddDays(1)));

        result.Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_OutroCartao_Deve_CriarOutraDespesa([Frozen] IdUsuario idUsuario, Cartao cartao, Cartao outroCartao)
    {
        DespesaParcelada existente = CriarDaFatura(idUsuario, outroCartao, parceladas: [], Compra()).Single();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, [existente], Compra());

        result.Should().ContainSingle();
    }

    [Theory]
    [InlineDomainAutoData(0, 2)]
    [InlineDomainAutoData(1, 1)]
    [InlineDomainAutoData(2, 0)]
    public void CriarDaFatura_Quando_ComprasGemeas_Deve_CriarSoAsQueFaltam(
        int quantidadeExistentes,
        int quantidadeEsperada,
        [Frozen] IdUsuario idUsuario,
        Cartao cartao)
    {
        DespesaParcelada[] existentes = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(), Compra())
            .Take(quantidadeExistentes)
            .ToArray();

        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, existentes, Compra(), Compra());

        result.Should().HaveCount(quantidadeEsperada);
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_EmpateEntreNumeros_Deve_UsarOMenor([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IReadOnlyList<DespesaParcelada> result = CriarDaFatura(idUsuario, cartao, parceladas: [], Compra(numero: 4), Compra(numero: 3));

        result.Should().ContainSingle().Which.Parcelas.Count(p => p.Pagamento.EstaPago).Should().Be(2);
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(IdUsuario idUsuario, Cartao cartao)
    {
        Action act = () => CriarDaFatura(idUsuario, cartao, parceladas: [], Compra());

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartao");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_BeneficiarioDeOutroUsuario_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IdUsuario outroUsuario = IdUsuario.FromNewVersion7Guid();
        Lancamento compra = Compra();
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(outroUsuario, [compra], TipoOrigem.Cartao),
            CategoriasResolvidas.Dos(idUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CategoriaDeOutroUsuario_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        IdUsuario outroUsuario = IdUsuario.FromNewVersion7Guid();
        Lancamento compra = Compra();
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            BeneficiariosResolvidos.Dos(idUsuario, [compra], TipoOrigem.Cartao),
            CategoriasResolvidas.Dos(outroUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_ParceladaDeOutroUsuario_Deve_LancarExcecao(
        [Frozen] IdUsuario idUsuario,
        Cartao cartao,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        ReaisPositivo valor)
    {
        DespesaParcelada deOutroUsuario = DespesaParcelada.Criar(
            IdUsuario.FromNewVersion7Guid(),
            idBeneficiario,
            descricao,
            idCategoria: null,
            _dataCompra,
            valor,
            cartao.Id,
            VencimentoFaturaCartao.MesVencimento(cartao, _dataCompra),
            cartao.DiaVencimento,
            (Parcela)(1, 2));

        Action act = () => CriarDaFatura(idUsuario, cartao, [deOutroUsuario], Compra());

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("parceladasDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_BeneficiarioNaoEncontrado_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        Lancamento compra = Compra();
        Action act = () => DespesaParceladaFactory.CriarDaFatura(
            idUsuario,
            beneficiariosDoUsuario: [],
            CategoriasResolvidas.Dos(idUsuario, [compra]),
            cartao,
            parceladasDoUsuario: [],
            [compra]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CategoriaNaoEncontrada_Deve_LancarExcecao([Frozen] IdUsuario idUsuario, Cartao cartao)
    {
        Lancamento compra = Compra();
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
            IdentidadeLancamento.Unica(Guid.NewGuid().ToString()).Value);
    }
}
