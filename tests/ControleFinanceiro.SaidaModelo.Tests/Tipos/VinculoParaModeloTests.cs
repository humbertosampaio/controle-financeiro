using System.Text.Json.Nodes;
using AutoFixture.Xunit2;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class VinculoParaModeloTests
{
    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        JsonObject json = VinculoParaModelo.De(transacao, despesa).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("transacao", "despesa", "valorProjetado", "resumo");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_LevarADespesaPagaPelaTransacaoEOValorProjetado(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        JsonObject json = VinculoParaModelo.De(transacao, despesa).Serializar();

        VinculoLido vinculo = new(json);
        vinculo.Transacao.Id.Should().Be(transacao.Id.Value);
        vinculo.Despesa.Id.Should().Be(despesa.Id.Value);
        vinculo.Despesa.Mes.Should().Be(despesa.MesReferencia.ToString());
        vinculo.Despesa.Status.Should().Be(StatusPagamento.Pago.Name);
        vinculo.ValorProjetado.Should().Be(despesa.Pagamento.ValorProjetado);
        vinculo.Resumo.Should().Be(VinculoParaModelo.De(transacao, despesa).Resumo);
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Quando_CompraCobraAParcela_Deve_LevarAParcelaPendenteComOValorCobrado(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Transacao transacao = Cobrar(despesa, cartao, transacoes, lancamento);

        JsonObject json = VinculoParaModelo.De(transacao, despesa).Serializar();

        VinculoLido vinculo = new(json);
        vinculo.Despesa.Status.Should().Be(StatusPagamento.Pendente.Name);
        vinculo.Despesa.Valor.Should().Be(transacao.ValorAbsoluto);
        vinculo.Despesa.Mes.Should().Be(despesa.Parcelas[^1].MesReferencia.ToString());
    }

    [Fact]
    public void Serializar_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = VinculoParaModelo.Falha(ErroParaModelo.ResumoDivergente).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
        ErroLido.DoEnvelope(json).Mensagem.Should().Be(ErroParaModelo.ResumoDivergente.Mensagem);
    }

    [Theory, UsuarioUnicoAutoData]
    public void Resumo_Deve_DescreverATransacaoEADespesaSemOsIds(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        string? resumo = VinculoParaModelo.De(transacao, despesa).Resumo;

        resumo.Should()
            .Contain(ComEspacosComuns(transacao.ValorAbsoluto.ToString()))
            .And.Contain(transacao.Descricao)
            .And.Contain(despesa.Descricao)
            .And.Contain(despesa.MesReferencia.ToString())
            .And.Contain(ComEspacosComuns(despesa.Pagamento.ValorProjetado.ToString()))
            .And.NotContain(transacao.Id.Value.ToString())
            .And.NotContain(despesa.Id.Value.ToString());
    }

    [Theory, UsuarioUnicoAutoData]
    public void Resumo_Quando_SaidaPagaASimples_Deve_DizerQueASaidaPaga(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        string? resumo = VinculoParaModelo.De(transacao, despesa).Resumo;

        resumo.Should().StartWith("A saída de").And.Contain(", paga a despesa ");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Resumo_Quando_CompraCobraAParcela_Deve_DizerQueACompraCobra(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Transacao transacao = Cobrar(despesa, cartao, transacoes, lancamento);

        string? resumo = VinculoParaModelo.De(transacao, despesa).Resumo;

        resumo.Should().StartWith("A compra de").And.Contain(", cobra a despesa ");
    }

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjPuro)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Resumo_Quando_TextosComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        TransacoesDeTeste transacoes,
        Lancamento lancamento,
        Fixture fixture)
    {
        TextoObrigatorio comDocumento = (TextoObrigatorio)$"LOJA {documentos.Em(forma)}";
        fixture.Inject(comDocumento);
        DespesaSimples despesa = fixture.Create<DespesaSimples>();
        Transacao transacao = Vincular(despesa, transacoes, lancamento.ComTextos(comDocumento, $"FULANO {documentos.Em(forma)}"));

        string? resumo = VinculoParaModelo.De(transacao, despesa).Resumo;

        resumo.Should().NotContain(documentos.Em(forma)).And.Contain($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    // O Reais formata com espaço não separável, e o resumo volta ao servidor nos argumentos da confirmação.
    [Theory, UsuarioUnicoAutoData]
    public void Resumo_Deve_TerSoEspacosComuns(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        string? resumo = VinculoParaModelo.De(transacao, despesa).Resumo;

        resumo.Should().NotContain(EspacoNaoSeparavel);
    }

    [Theory, UsuarioUnicoAutoData]
    public void ConfereResumo_Quando_ResumoIgual_Deve_Conferir(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        VinculoParaModelo vinculo = VinculoParaModelo.De(Vincular(despesa, transacoes, lancamento), despesa);

        vinculo.ConfereResumo(vinculo.Resumo!).Should().BeTrue();
    }

    [Theory, UsuarioUnicoAutoData]
    public void ConfereResumo_Quando_EspacosNaoSeparaveisENasPontas_Deve_Conferir(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        VinculoParaModelo vinculo = VinculoParaModelo.De(Vincular(despesa, transacoes, lancamento), despesa);

        vinculo.ConfereResumo($" {vinculo.Resumo!.Replace(" ", EspacoNaoSeparavel, StringComparison.Ordinal)} ").Should().BeTrue();
    }

    [Theory, UsuarioUnicoAutoData]
    public void ConfereResumo_Quando_ResumoAlterado_Deve_NaoConferir(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        VinculoParaModelo vinculo = VinculoParaModelo.De(Vincular(despesa, transacoes, lancamento), despesa);

        vinculo.ConfereResumo(vinculo.Resumo!.Replace("despesa", "despesas", StringComparison.Ordinal)).Should().BeFalse();
    }

    [Fact]
    public void ConfereResumo_Quando_Falha_Deve_NaoConferir()
    {
        VinculoParaModelo.Falha(ErroParaModelo.DespesaJaPaga).ConfereResumo(string.Empty).Should().BeFalse();
    }

    [Theory, UsuarioUnicoAutoData]
    public void De_Quando_TransacaoNaoPagouADespesa_Deve_LancarExcecao(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());

        Action act = () => VinculoParaModelo.De(transacao, despesa);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesa");
    }

    private const string EspacoNaoSeparavel = " ";

    private static string ComEspacosComuns(string texto) => texto.Replace(EspacoNaoSeparavel, " ", StringComparison.Ordinal);

    /// <summary>A compra da última parcela da despesa, no cartão dela, ligada à despesa: a parcela fica cobrada e pendente.</summary>
    private static Transacao Cobrar(DespesaParcelada despesa, Cartao cartao, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Lancamento compra = Lancamento.DoCartao(
            despesa.DataOcorrencia,
            hora: null,
            -(decimal.Abs(lancamento.Valor) + 1m),
            despesa.Descricao,
            contraparte: null,
            (Parcela)(despesa.Parcelas.Count, despesa.Parcelas.Count),
            categoriaBanco: null,
            lancamento.Identidade);
        Transacao transacao = transacoes.DoCartao(cartao, compra);
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        return transacao;
    }

    private static Transacao Vincular(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());
        VinculoTransacaoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        return transacao;
    }
}
