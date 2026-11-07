using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
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

        json.Select(campo => campo.Key).Should().BeEquivalentTo("transacao", "despesa", "valorProjetado");
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
    }

    [Fact]
    public void Serializar_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = VinculoParaModelo.Falha(ErroParaModelo.ConfirmacaoRecusada).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
        ErroLido.DoEnvelope(json).Mensagem.Should().Be(ErroParaModelo.ConfirmacaoRecusada.Mensagem);
    }

    [Theory, UsuarioUnicoAutoData]
    public void Mensagem_Deve_DescreverATransacaoEADespesaSemOsIds(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = Vincular(despesa, transacoes, lancamento);

        string? mensagem = VinculoParaModelo.De(transacao, despesa).Mensagem;

        mensagem.Should()
            .Contain(transacao.ValorAbsoluto.ToString())
            .And.Contain(transacao.Descricao)
            .And.Contain(despesa.Descricao)
            .And.Contain(despesa.MesReferencia.ToString())
            .And.Contain(despesa.Pagamento.ValorProjetado.ToString())
            .And.NotContain(transacao.Id.Value.ToString())
            .And.NotContain(despesa.Id.Value.ToString());
    }

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjPuro)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Mensagem_Quando_TextosComDocumento_Deve_SairSemODocumento(
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

        string? mensagem = VinculoParaModelo.De(transacao, despesa).Mensagem;

        mensagem.Should().NotContain(documentos.Em(forma)).And.Contain($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, UsuarioUnicoAutoData]
    public void De_Quando_TransacaoNaoQuitouADespesa_Deve_LancarExcecao(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());

        Action act = () => VinculoParaModelo.De(transacao, despesa);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesa");
    }

    private static Transacao Vincular(DespesaSimples despesa, TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        return transacao;
    }
}
