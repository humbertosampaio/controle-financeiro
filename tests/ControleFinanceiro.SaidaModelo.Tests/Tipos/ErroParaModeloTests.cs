using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class ErroParaModeloTests
{
    public static readonly TheoryData<ErroParaModelo> Erros = new(
        ErroParaModelo.MesInvalido,
        ErroParaModelo.UsuarioNaoEncontrado,
        ErroParaModelo.TransacaoNaoEncontrada,
        ErroParaModelo.DespesaNaoEncontrada,
        ErroParaModelo.TransacaoJaVinculada,
        ErroParaModelo.DespesaJaPaga,
        ErroParaModelo.TransacaoNaoEhSaida,
        ErroParaModelo.OrigemIncompativel,
        ErroParaModelo.ParcelaIncompativel,
        ErroParaModelo.ResumoDivergente);

    public static readonly TheoryData<Erro, ErroParaModelo> ErrosDoVinculo = new()
    {
        { ErroDespesaJaPaga.Instancia, ErroParaModelo.DespesaJaPaga },
        { ErroTransacaoNaoEhSaida.Instancia, ErroParaModelo.TransacaoNaoEhSaida },
        { ErroOrigemIncompativelComDespesa.Instancia, ErroParaModelo.OrigemIncompativel },
        { ErroParcelaIncompativelComDespesa.Instancia, ErroParaModelo.ParcelaIncompativel },
    };

    [Theory, MemberData(nameof(Erros))]
    public void Serializar_Deve_TerSomenteAMensagem(ErroParaModelo erro)
    {
        JsonObject json = erro.Serializar();

        json.Select(campo => campo.Key).Should().Equal("mensagem");
        new ErroLido(json).Mensagem.Should().NotBeEmpty();
    }

    [Theory, MemberData(nameof(ErrosDoVinculo))]
    public void De_Quando_ErroDoVinculo_Deve_RetornarAMensagemFixa(Erro erro, ErroParaModelo esperado)
    {
        ErroParaModelo.De(erro).Should().BeSameAs(esperado);
    }

    // O erro só nasce no serviço, com a despesa que já usa a transação, e ela não vai à mensagem.
    [Theory, UsuarioUnicoAutoData]
    public void De_Quando_TransacaoJaVinculada_Deve_RetornarAMensagemFixa(
        DespesaSimples despesa,
        DespesaSimples outra,
        TransacoesDeTeste transacoes,
        Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria().ComoSaida());
        Erro erro = VinculoPagamentoService.Vincular(transacao, despesa, outra).Error;

        ErroParaModelo.De(erro).Should().BeSameAs(ErroParaModelo.TransacaoJaVinculada);
    }

    [Fact]
    public void De_Quando_ErroSemMensagemMapeada_Deve_LancarExcecao()
    {
        Action act = () => ErroParaModelo.De(ErroTransacaoNaoVinculada.Instancia);

        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
