using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class ConciliacaoParcelasServiceTests
{
    private const decimal ValorMenor = 100m;
    private const decimal ValorMaior = 200m;

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_ParcelaDaCompra_Deve_VincularSoOMesmoNumero(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela());

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:1, SemParcela:0));
        despesa.Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacao.Id);
        despesa.Parcelas.Where(p => p.NumeroParcela != 3).Should().OnlyContain(p => p.Cobranca == null);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_ParcelaSeguinteDaDespesaExistente_Deve_VincularNaMesmaDespesa(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao terceira = compra.Transacao(compra.Parcela());
        compra.Conciliar([despesa], [terceira]);
        Transacao quarta = compra.Transacao(compra.Parcela(numero: 4));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [terceira, quarta]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:1, SemParcela:0));
        despesa.Parcelas[2].Cobranca!.IdTransacao.Should().Be(terceira.Id);
        despesa.Parcelas[3].Cobranca!.IdTransacao.Should().Be(quarta.Id);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_ParcelaNasceuPaga_Deve_CobrarComOValorDaTransacaoEContinuarPaga(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Pagamento pagamento = despesa.Parcelas[0].Pagamento;
        Transacao transacao = compra.Transacao(compra.Parcela(numero: 1, valor: -ValorMaior));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Vinculadas.Should().Be(1);
        despesa.Parcelas[0].Cobranca.Should().Be(new Cobranca(transacao.Id, (ReaisPositivo)ValorMaior));
        despesa.Parcelas[0].Pagamento.Should().Be(pagamento);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_GemeasDeMesmoValor_Deve_UsarTransacoesDistintas(CompraDeTeste compra)
    {
        IReadOnlyList<DespesaParcelada> gemeas = compra.Despesas(compra.Parcela(), compra.Parcela());
        Transacao primeira = compra.Transacao(compra.Parcela());
        Transacao segunda = compra.Transacao(compra.Parcela());

        ConciliacaoParcelas result = compra.Conciliar(gemeas, [primeira, segunda]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:2, SemParcela:0));
        gemeas.Select(d => d.Parcelas[2].Cobranca?.IdTransacao).Should().BeEquivalentTo([primeira.Id, segunda.Id]);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_GemeasDeValoresDiferentes_Deve_CasarPeloValorNosDoisMeses(CompraDeTeste compra)
    {
        (DespesaParcelada menor, DespesaParcelada maior) = compra.GemeasDeValoresDiferentes();
        Transacao terceiraMaior = compra.Transacao(compra.Parcela(valor: -ValorMaior));
        Transacao terceiraMenor = compra.Transacao(compra.Parcela(valor: -ValorMenor));
        Transacao quartaMaior = compra.Transacao(compra.Parcela(numero: 4, valor: -ValorMaior));
        Transacao quartaMenor = compra.Transacao(compra.Parcela(numero: 4, valor: -ValorMenor));

        compra.Conciliar([menor, maior], [terceiraMaior, terceiraMenor, quartaMaior, quartaMenor]);

        menor.Parcelas[2].Cobranca!.IdTransacao.Should().Be(terceiraMenor.Id);
        menor.Parcelas[3].Cobranca!.IdTransacao.Should().Be(quartaMenor.Id);
        maior.Parcelas[2].Cobranca!.IdTransacao.Should().Be(terceiraMaior.Id);
        maior.Parcelas[3].Cobranca!.IdTransacao.Should().Be(quartaMaior.Id);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_GemeasDeMesmoValor_Deve_DesempatarPeloIdEmQualquerOrdemDasListas(CompraDeTeste compra)
    {
        DespesaParcelada[] gemeas = compra.Despesas(compra.Parcela(), compra.Parcela()).OrderBy(d => d.Id).ToArray();
        Transacao[] transacoes = new[] { compra.Transacao(compra.Parcela()), compra.Transacao(compra.Parcela()) }.OrderBy(t => t.Id).ToArray();

        compra.Conciliar(gemeas.Reverse().ToArray(), transacoes.Reverse().ToArray());

        gemeas[0].Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacoes[0].Id);
        gemeas[1].Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacoes[1].Id);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_SoATransacaoDoValorMaior_Deve_CasarPeloValorENaoPelaOrdem(CompraDeTeste compra)
    {
        (DespesaParcelada menor, DespesaParcelada maior) = compra.GemeasDeValoresDiferentes();
        Transacao transacao = compra.Transacao(compra.Parcela(valor: -ValorMaior));

        compra.Conciliar([menor, maior], [transacao]);

        maior.Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacao.Id);
        menor.Parcelas[2].Cobranca.Should().BeNull();
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_UmaTemValorIgualEOutraNao_Deve_CasarOValorIgualEDepoisOQueSobrou(CompraDeTeste compra)
    {
        (DespesaParcelada menor, DespesaParcelada maior) = compra.GemeasDeValoresDiferentes();
        Transacao valorIgual = compra.Transacao(compra.Parcela(valor: -ValorMaior));
        Transacao semValorIgual = compra.Transacao(compra.Parcela(valor: -(ValorMaior + 50m)));

        compra.Conciliar([menor, maior], [valorIgual, semValorIgual]);

        maior.Parcelas[2].Cobranca!.IdTransacao.Should().Be(valorIgual.Id);
        menor.Parcelas[2].Cobranca!.IdTransacao.Should().Be(semValorIgual.Id);
    }

    [Theory]
    [InlineConciliacaoAutoData(false)]
    [InlineConciliacaoAutoData(true)]
    public void Conciliar_Quando_GemeasSemValorIgual_Deve_CasarNaOrdemDeValorEmQualquerOrdemDasListas(bool inverter, CompraDeTeste compra)
    {
        (DespesaParcelada menor, DespesaParcelada maior) = compra.GemeasDeValoresDiferentes();
        Transacao transacaoMenor = compra.Transacao(compra.Parcela(valor: -(ValorMenor + 1m)));
        Transacao transacaoMaior = compra.Transacao(compra.Parcela(valor: -(ValorMaior + 1m)));
        DespesaParcelada[] despesas = inverter ? [maior, menor] : [menor, maior];
        Transacao[] transacoes = inverter ? [transacaoMaior, transacaoMenor] : [transacaoMenor, transacaoMaior];

        compra.Conciliar(despesas, transacoes);

        menor.Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacaoMenor.Id);
        maior.Parcelas[2].Cobranca!.IdTransacao.Should().Be(transacaoMaior.Id);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_ChamadaDeNovo_Deve_NaoVincularNemMudarNada(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela());
        compra.Conciliar([despesa], [transacao]);
        Cobranca?[] cobrancas = despesa.Parcelas.Select(p => p.Cobranca).ToArray();

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:0));
        despesa.Parcelas.Select(p => p.Cobranca).Should().Equal(cobrancas);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_VinculoExistente_Deve_PreservarEUsarATransacaoLivreNaOutraGemea(CompraDeTeste compra)
    {
        IReadOnlyList<DespesaParcelada> gemeas = compra.Despesas(compra.Parcela(), compra.Parcela());
        Transacao vinculada = compra.Transacao(compra.Parcela());
        Transacao livre = compra.Transacao(compra.Parcela());
        VinculoTransacaoService.Vincular(vinculada, gemeas[1], despesaQueUsaATransacao: Maybe<Despesa>.None, cartoesDoUsuario: []).Should().Succeed();

        ConciliacaoParcelas result = compra.Conciliar(gemeas, [vinculada, livre]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:1, SemParcela:0));
        gemeas[1].Parcelas[2].Cobranca!.IdTransacao.Should().Be(vinculada.Id);
        gemeas[0].Parcelas[2].Cobranca!.IdTransacao.Should().Be(livre.Id);
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_TransacaoAMais_Deve_ContarEmSemParcela(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();

        ConciliacaoParcelas result = compra.Conciliar(
            [despesa],
            [compra.Transacao(compra.Parcela()), compra.Transacao(compra.Parcela())]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:1, SemParcela:1));
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_OutraData_Deve_ContarEmSemParcela(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela(data: despesa.DataOcorrencia.AddDays(1)));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:1));
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_OutraDescricao_Deve_ContarEmSemParcela(CompraDeTeste compra, TextoObrigatorio outraDescricao)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela(descricao: outraDescricao));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:1));
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_OutroTotal_Deve_ContarEmSemParcela(CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela(total: 12));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:1));
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_OutroCartao_Deve_ContarEmSemParcela(CompraDeTeste compra, Cartao outroCartao, TransacoesDeTeste transacoes)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = transacoes.DoCartao(outroCartao, compra.Parcela());

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:1));
    }

    [Theory]
    [InlineConciliacaoAutoData("loja de exemplo")]
    [InlineConciliacaoAutoData("LÓJA DE EXEMPLO")]
    [InlineConciliacaoAutoData("  LOJA   DE  EXEMPLO ")]
    public void Conciliar_Quando_DescricaoDifereNaForma_Deve_ReconhecerAMesmaCompra(string descricao, CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela(descricao: (TextoObrigatorio)"LOJA DE EXEMPLO")).Single();
        Transacao transacao = compra.Transacao(compra.Parcela(descricao: (TextoObrigatorio)descricao));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Vinculadas.Should().Be(1);
    }

    [Theory]
    [InlineConciliacaoAutoData(50.00)]
    [InlineConciliacaoAutoData(0.00)]
    public void Conciliar_Quando_TransacaoNaoEhSaida_Deve_IgnorarSemContar(decimal valor, CompraDeTeste compra)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(compra.Parcela(valor: valor));

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:0));
    }

    [Theory, ConciliacaoAutoData]
    public void Conciliar_Quando_TransacaoAVista_Deve_IgnorarSemContar(CompraDeTeste compra, VinculoTransacaoServiceTests.SaidaDeTeste saida)
    {
        DespesaParcelada despesa = compra.Despesas(compra.Parcela()).Single();
        Transacao transacao = compra.Transacao(saida.AVista());

        ConciliacaoParcelas result = compra.Conciliar([despesa], [transacao]);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:0));
    }

    [Theory, DomainAutoData]
    public void Conciliar_Quando_DespesaDeOutroUsuario_Deve_LancarExcecao(DespesaParcelada deOutroUsuario, IdUsuario idUsuario)
    {
        Action act = () => ConciliacaoParcelasService.Conciliar(idUsuario, [deOutroUsuario], transacoesParceladas: []);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("parceladas");
    }

    // O id vem antes do [Frozen]: só o que é pedido depois dele sai do usuário congelado.
    [Theory, DomainAutoData]
    public void Conciliar_Quando_TransacaoDeOutroUsuario_Deve_LancarExcecao(IdUsuario idUsuario, [Frozen] IdUsuario outroUsuario, CompraDeTeste compra)
    {
        Transacao deOutroUsuario = compra.Transacao(compra.Parcela());
        deOutroUsuario.IdUsuario.Should().Be(outroUsuario);

        Action act = () => ConciliacaoParcelasService.Conciliar(idUsuario, parceladas: [], [deOutroUsuario]);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacoesParceladas");
    }

    [Theory, DomainAutoData]
    public void Conciliar_Quando_ListasVazias_Deve_RetornarContagensZeradas(IdUsuario idUsuario)
    {
        ConciliacaoParcelas result = ConciliacaoParcelasService.Conciliar(idUsuario, parceladas: [], transacoesParceladas: []);

        result.Should().Be(new ConciliacaoParcelas(Vinculadas:0, SemParcela:0));
    }

    /// <summary>Uma compra num cartão, com os lançamentos, as despesas e as transações que os testes derivam dela.</summary>
    public sealed class CompraDeTeste
    {
        private readonly Cartao _cartao;
        private readonly TransacoesDeTeste _transacoes;
        private readonly DateOnly _data;
        private readonly TextoObrigatorio _descricao;
        private readonly IdentidadeLancamento _identidade;

        public CompraDeTeste(Cartao cartao, TransacoesDeTeste transacoes, DateOnly data, TextoObrigatorio descricao, IdentidadeLancamento identidade)
        {
            _cartao = cartao;
            _transacoes = transacoes;
            _data = data;
            _descricao = descricao;
            _identidade = identidade;
        }

        /// <summary>Por padrão, a parcela 3 de 10: a despesa criada dela tem parcelas pagas antes e pendentes depois.</summary>
        public Lancamento Parcela(int numero = 3, int total = 10, decimal valor = -ValorMenor, DateOnly? data = null, TextoObrigatorio? descricao = null)
        {
            return Lancamento.DoCartao(
                data ?? _data,
                hora: null,
                valor,
                descricao ?? _descricao,
                contraparte: null,
                (Parcela)(numero, total),
                categoriaBanco: null,
                _identidade);
        }

        /// <summary>Uma despesa por lançamento: repetir a mesma chave cria compras gêmeas.</summary>
        public IReadOnlyList<DespesaParcelada> Despesas(params Lancamento[] lancamentos)
        {
            IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(_cartao.IdUsuario, lancamentos);
            return DespesaParceladaFactory.CriarDaFatura(_cartao.IdUsuario, beneficiarios, categoriasDoUsuario: [], _cartao, parceladasDoUsuario: [], lancamentos);
        }

        public (DespesaParcelada Menor, DespesaParcelada Maior) GemeasDeValoresDiferentes()
        {
            IReadOnlyList<DespesaParcelada> gemeas = Despesas(Parcela(valor: -ValorMenor), Parcela(valor: -ValorMaior));
            return (gemeas[0], gemeas[1]);
        }

        public Transacao Transacao(Lancamento lancamento) => _transacoes.DoCartao(_cartao, lancamento);

        public ConciliacaoParcelas Conciliar(IReadOnlyCollection<DespesaParcelada> despesas, IReadOnlyCollection<Transacao> transacoes) =>
            ConciliacaoParcelasService.Conciliar(_cartao.IdUsuario, despesas, transacoes);
    }

    /// <summary>Tudo sai do mesmo usuário.</summary>
    private sealed class ConciliacaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public ConciliacaoAutoDataAttribute()
            : base(fixture => fixture.Freeze<IdUsuario>())
        {
        }
    }

    private sealed class InlineConciliacaoAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineConciliacaoAutoDataAttribute(params object[] valores)
            : base(new ConciliacaoAutoDataAttribute(), valores)
        {
        }
    }
}
