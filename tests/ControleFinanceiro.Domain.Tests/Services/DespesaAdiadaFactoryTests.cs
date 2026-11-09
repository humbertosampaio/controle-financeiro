using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class DespesaAdiadaFactoryTests
{
    private const string DescricaoCompra = "LOJA DE EXEMPLO";
    private const string CategoriaCompra = "Eletrônicos";
    private static readonly DateOnly _dataCompra = new(2026, 03, 15);

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CompraAVistaSemPrevisao_Deve_CriarAdiadaCobradaEPendente(
        IdUsuario idUsuario,
        Cartao cartao,
        FaturaDeTeste fatura,
        Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], novas);

        DespesaAdiada despesa = result.Should().ContainSingle().Subject;
        MesReferencia mesVencimento = cartao.MesVencimento(_dataCompra);
        despesa.IdUsuario.Should().Be(idUsuario);
        despesa.IdBeneficiario.Should().Be(fatura.Beneficiarios.Single().Id);
        despesa.IdCategoria.Should().Be(fatura.Categorias.Single().Id);
        despesa.IdCartao.Should().Be(cartao.Id);
        despesa.IdDespesaRecorrente.Should().BeNull();
        despesa.Descricao.Should().Be((TextoObrigatorio)DescricaoCompra);
        despesa.DataOcorrencia.Should().Be(_dataCompra);
        despesa.MesReferencia.Should().Be(mesVencimento);
        despesa.Cobranca.Should().Be(new Cobranca(novas.Single().Id, (ReaisPositivo)99.90m));
        despesa.Pagamento.EstaPago.Should().BeFalse();
        despesa.Pagamento.ValorProjetado.Should().Be((ReaisPositivo)99.90m);
        despesa.Pagamento.DataProjetada.Should().Be(mesVencimento.NoDia(cartao.DiaVencimento));
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CompraParcelada_Deve_NaoCriar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, Compra(compra.Identidade, parcela: (Parcela)(3, 10)));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], novas);

        result.Should().BeEmpty();
    }

    /// <remarks>O pagamento da fatura e o estorno chegam no cartão como entrada.</remarks>
    [Theory]
    [InlineCompraAutoData(99.90)]
    [InlineCompraAutoData(0)]
    public void CriarDaFatura_Quando_NaoEhSaida_Deve_NaoCriar(decimal valor, Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, Compra(compra.Identidade, valor: valor));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], novas);

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_DescricaoNormalizaParaVazio_Deve_NaoCriar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, Compra(compra.Identidade, descricao: "\u0301"));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], novas);

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_SemCategoriaDoBanco_Deve_CriarSemCategoria(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, Compra(compra.Identidade, categoria: null));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], novas);

        result.Should().ContainSingle().Which.IdCategoria.Should().BeNull();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoNoMesmoCartao_Deve_NaoCriarNemCobrarAPrevisao(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);
        DespesaAdiada previsao = fatura.PrevisaoDe(cartao, compra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], [previsao], novas);

        result.Should().BeEmpty();
        previsao.Cobranca.Should().BeNull();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoEmOutroFinalDaMesmaFatura_Deve_NaoCriar(
        Cartao cartao,
        SufixoCartao outroSufixo,
        FaturaDeTeste fatura,
        Lancamento compra)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao, outroFinal], [fatura.PrevisaoDe(outroFinal, compra)], novas);

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoEmCartaoDeOutraFatura_Deve_Criar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        Cartao outroBanco = cartao.EmOutroBanco();
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao, outroBanco], [fatura.PrevisaoDe(outroBanco, compra)], novas);

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoDeOutroBeneficiario_Deve_Criar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, Compra(compra.Identidade, descricao: "OUTRA LOJA"));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], [fatura.PrevisaoDe(cartao, compra)], novas);

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoDeOutroMes_Deve_Criar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);
        Lancamento noMesSeguinte = Compra(compra.Identidade, data: _dataCompra.AddMonths(1));

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], [fatura.PrevisaoDe(cartao, noMesSeguinte)], novas);

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_PrevisaoJaCobrada_Deve_Criar(Cartao cartao, FaturaDeTeste fatura, Lancamento compra, Lancamento outraCompra)
    {
        DespesaAdiada jaCobrada = fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], fatura.Importar(cartao, compra)).Single();
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, outraCompra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], [jaCobrada], novas);

        result.Should().ContainSingle();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_DuasComprasEUmaPrevisao_Deve_CriarSoADaSegunda(
        Cartao cartao,
        FaturaDeTeste fatura,
        Lancamento compra,
        Lancamento outraCompra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra, outraCompra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao], [fatura.PrevisaoDe(cartao, compra)], novas);

        result.Should().ContainSingle().Which.Cobranca!.IdTransacao.Should().Be(novas[1].Id);
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_DuasComprasEmFinaisDaMesmaFaturaEUmaPrevisao_Deve_CriarUma(
        Cartao cartao,
        SufixoCartao outroSufixo,
        FaturaDeTeste fatura,
        Lancamento compra,
        Lancamento outraCompra)
    {
        Cartao outroFinal = cartao.OutroFinal(outroSufixo);
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra)
            .Concat(fatura.Importar(outroFinal, outraCompra))
            .ToArray();

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas([cartao, outroFinal], [fatura.PrevisaoDe(cartao, compra)], novas);

        result.Should().ContainSingle().Which.IdCartao.Should().Be(outroFinal.Id);
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_DuasComprasEDuasPrevisoes_Deve_NaoCriar(
        Cartao cartao,
        FaturaDeTeste fatura,
        Lancamento compra,
        Lancamento outraCompra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra, outraCompra);

        IReadOnlyList<DespesaAdiada> result = fatura.CriarAdiadas(
            [cartao],
            [fatura.PrevisaoDe(cartao, compra), fatura.PrevisaoDe(cartao, outraCompra)],
            novas);

        result.Should().BeEmpty();
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_TransacaoDaConta_Deve_LancarExcecao(TransacoesDeTeste transacoes, Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        Transacao daConta = transacoes.DaConta(compra.SemCategoria());

        Action act = () => fatura.CriarAdiadas([cartao], adiadasDoUsuario: [], [daConta]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacoesNovas");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CartaoDaCompraForaDaLista_Deve_LancarExcecao(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => fatura.CriarAdiadas(cartoesDoUsuario: [], adiadasDoUsuario: [], novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_BeneficiarioNaoEncontrado_Deve_LancarExcecao(IdUsuario idUsuario, Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => DespesaAdiadaFactory.CriarDaFatura(
            idUsuario,
            beneficiariosDoUsuario: [],
            fatura.Categorias,
            [cartao],
            adiadasDoUsuario: [],
            novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CategoriaNaoEncontrada_Deve_LancarExcecao(IdUsuario idUsuario, Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => DespesaAdiadaFactory.CriarDaFatura(
            idUsuario,
            fatura.Beneficiarios,
            categoriasDoUsuario: [],
            [cartao],
            adiadasDoUsuario: [],
            novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_BeneficiarioDeOutroUsuario_Deve_LancarExcecao(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => DespesaAdiadaFactory.CriarDaFatura(
            cartao.IdUsuario,
            BeneficiariosResolvidos.Dos(IdUsuario.FromNewVersion7Guid(), [compra]),
            fatura.Categorias,
            [cartao],
            adiadasDoUsuario: [],
            novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("beneficiariosDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_CategoriaDeOutroUsuario_Deve_LancarExcecao(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => DespesaAdiadaFactory.CriarDaFatura(
            cartao.IdUsuario,
            fatura.Beneficiarios,
            CategoriasResolvidas.Dos(IdUsuario.FromNewVersion7Guid(), [compra]),
            [cartao],
            adiadasDoUsuario: [],
            novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoriasDoUsuario");
    }

    // O cartão e a despesa vêm antes do [Frozen]: só o que é pedido depois dele sai do usuário congelado.
    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_CartaoDeOutroUsuario_Deve_LancarExcecao(
        Cartao deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        FaturaDeTeste fatura)
    {
        deOutroUsuario.IdUsuario.Should().NotBe(idUsuario);

        Action act = () => fatura.CriarAdiadas([deOutroUsuario], adiadasDoUsuario: [], transacoesNovas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("cartoesDoUsuario");
    }

    [Theory, DomainAutoData]
    public void CriarDaFatura_Quando_AdiadaDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        FaturaDeTeste fatura)
    {
        deOutroUsuario.IdUsuario.Should().NotBe(idUsuario);

        Action act = () => fatura.CriarAdiadas(cartoesDoUsuario: [], [deOutroUsuario], transacoesNovas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("adiadasDoUsuario");
    }

    [Theory, CompraAutoData]
    public void CriarDaFatura_Quando_TransacaoDeOutroUsuario_Deve_LancarExcecao(Cartao cartao, FaturaDeTeste fatura, Lancamento compra)
    {
        IReadOnlyList<Transacao> novas = fatura.Importar(cartao, compra);

        Action act = () => DespesaAdiadaFactory.CriarDaFatura(
            IdUsuario.FromNewVersion7Guid(),
            beneficiariosDoUsuario: [],
            categoriasDoUsuario: [],
            cartoesDoUsuario: [],
            adiadasDoUsuario: [],
            novas);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacoesNovas");
    }

    /// <param name="parcela">Nula na compra à vista.</param>
    private static Lancamento Compra(
        IdentidadeLancamento identidade,
        Parcela? parcela = null,
        decimal valor = -99.90m,
        DateOnly? data = null,
        string descricao = DescricaoCompra,
        string? categoria = CategoriaCompra)
    {
        return Lancamento.DoCartao(
            data ?? _dataCompra,
            hora: null,
            valor,
            (TextoObrigatorio)descricao,
            contraparte: null,
            parcela,
            categoria,
            identidade);
    }

    /// <summary>
    /// A fatura importada no teste: grava as compras de cada cartão pela <see cref="LoteTransacoesFactory"/> e guarda os
    /// beneficiários e as categorias delas, os mesmos que a <see cref="DespesaAdiadaFactory"/> e a previsão recebem.
    /// </summary>
    public sealed class FaturaDeTeste
    {
        private readonly IdUsuario _idUsuario;
        private readonly LayoutArquivoImportacao _layout;
        private readonly PeriodoArquivo _periodo;
        private readonly DateTimeOffset _importadaEm;
        private readonly List<Beneficiario> _beneficiarios = [];
        private readonly List<CategoriaDespesa> _categorias = [];

        public IReadOnlyList<Beneficiario> Beneficiarios => _beneficiarios;
        public IReadOnlyList<CategoriaDespesa> Categorias => _categorias;

        public FaturaDeTeste(IdUsuario idUsuario, LayoutArquivoImportacao layout, PeriodoArquivo periodo, DateTimeOffset importadaEm)
        {
            _idUsuario = idUsuario;
            _layout = layout;
            _periodo = periodo;
            _importadaEm = importadaEm;
        }

        public IReadOnlyList<Transacao> Importar(Cartao cartao, params Lancamento[] compras)
        {
            Resolver(compras);
            ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(cartao, _layout, _periodo, compras.Length, _importadaEm);

            return LoteTransacoesFactory.Criar(_idUsuario, _beneficiarios, _categorias, cartao, importacao, compras).Value;
        }

        public IReadOnlyList<DespesaAdiada> CriarAdiadas(
            IReadOnlyCollection<Cartao> cartoesDoUsuario,
            IReadOnlyCollection<DespesaAdiada> adiadasDoUsuario,
            IReadOnlyList<Transacao> transacoesNovas)
        {
            return DespesaAdiadaFactory.CriarDaFatura(_idUsuario, _beneficiarios, _categorias, cartoesDoUsuario, adiadasDoUsuario, transacoesNovas);
        }

        /// <summary>A adiada que prevê a compra no cartão: mesmo beneficiário, no mês de vencimento da data dela, sem cobrança.</summary>
        public DespesaAdiada PrevisaoDe(Cartao cartao, Lancamento compra)
        {
            Resolver([compra]);
            Beneficiario beneficiario = _beneficiarios.Single(b => b.PossuiAlias(compra.AliasTextoBeneficiario!));

            return DespesaAdiada.Criar(
                _idUsuario,
                beneficiario.Id,
                compra.Descricao,
                idCategoria: null,
                compra.Data,
                compra.ValorAbsoluto,
                cartao.Id,
                cartao.VencimentoDaCompra(compra.Data),
                idDespesaRecorrente: null);
        }

        private void Resolver(IReadOnlyCollection<Lancamento> compras)
        {
            _beneficiarios.AddRange(BeneficiarioFactory.Resolver(_idUsuario, _beneficiarios, Novos(compras)).Value);
            _categorias.AddRange(CategoriaDespesaFactory.Resolver(_idUsuario, _categorias, NovasCategorias(compras)).Value);
        }

        private IEnumerable<BeneficiarioNovo> Novos(IReadOnlyCollection<Lancamento> compras)
        {
            return AliasesBeneficiarioPendentes
                .Listar(_idUsuario, _beneficiarios, compras)
                .Select(alias => new BeneficiarioNovo((TextoObrigatorio)alias.TextoOriginal, alias));
        }

        private IEnumerable<CategoriaNova> NovasCategorias(IReadOnlyCollection<Lancamento> compras)
        {
            return AliasesCategoriaPendentes
                .Listar(_idUsuario, _categorias, compras)
                .Select(alias => new CategoriaNova((TextoObrigatorio)alias.TextoOriginal, alias));
        }
    }

    /// <summary>Tudo sai do mesmo usuário, e o <see cref="Lancamento"/> do teste é a compra à vista padrão.</summary>
    private sealed class CompraAutoDataAttribute : DomainAutoDataAttribute
    {
        public CompraAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Register((IdentidadeLancamento identidade) => Compra(identidade));
            })
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
