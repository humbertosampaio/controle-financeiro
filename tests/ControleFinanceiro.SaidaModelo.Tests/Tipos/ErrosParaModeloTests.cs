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

public sealed class ErrosParaModeloTests
{
    public static readonly TheoryData<ErroParaModelo> Erros = new(
        ErrosParaModelo.Consulta.MesInvalido,
        ErrosParaModelo.Consulta.UsuarioNaoEncontrado,
        ErrosParaModelo.Consulta.TransacaoNaoEncontrada,
        ErrosParaModelo.Consulta.DespesaNaoEncontrada,
        ErrosParaModelo.Consulta.CartaoNaoEncontrado,
        ErrosParaModelo.Vinculo.TransacaoJaVinculada,
        ErrosParaModelo.Vinculo.DespesaJaPaga,
        ErrosParaModelo.Vinculo.DespesaJaCobrada,
        ErrosParaModelo.Vinculo.TransacaoNaoEhSaida,
        ErrosParaModelo.Vinculo.OrigemIncompativel,
        ErrosParaModelo.Vinculo.ParcelaIncompativel,
        ErrosParaModelo.PagamentoFatura.TransacaoPagaOutraFatura,
        ErrosParaModelo.PagamentoFatura.FaturaSemCobrancaPendente,
        ErrosParaModelo.Vinculo.ResumoDivergente,
        ErrosParaModelo.PagamentoFatura.ResumoDivergente);

    public static readonly TheoryData<Erro, ErroParaModelo> ErrosDoDominio = new()
    {
        { ErroDespesaJaPaga.Instancia, ErrosParaModelo.Vinculo.DespesaJaPaga },
        { ErroDespesaJaCobrada.Instancia, ErrosParaModelo.Vinculo.DespesaJaCobrada },
        { ErroTransacaoNaoEhSaida.Instancia, ErrosParaModelo.Vinculo.TransacaoNaoEhSaida },
        { ErroOrigemIncompativelComDespesa.Instancia, ErrosParaModelo.Vinculo.OrigemIncompativel },
        { ErroParcelaIncompativelComDespesa.Instancia, ErrosParaModelo.Vinculo.ParcelaIncompativel },
        { ErroTransacaoPagaOutraFatura.Instancia, ErrosParaModelo.PagamentoFatura.TransacaoPagaOutraFatura },
        { ErroFaturaSemCobrancaPendente.Instancia, ErrosParaModelo.PagamentoFatura.FaturaSemCobrancaPendente },
    };

    [Theory, MemberData(nameof(Erros))]
    public void Serializar_Deve_TerSomenteAMensagem(ErroParaModelo erro)
    {
        JsonObject json = erro.Serializar();

        json.Select(campo => campo.Key).Should().Equal("mensagem");
        new ErroLido(json).Mensagem.Should().NotBeEmpty();
    }

    [Theory, MemberData(nameof(ErrosDoDominio))]
    public void De_Quando_ErroDoDominio_Deve_RetornarAMensagemFixa(Erro erro, ErroParaModelo esperado)
    {
        ErrosParaModelo.De(erro).Should().BeSameAs(esperado);
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
        Erro erro = VinculoTransacaoService.Vincular(transacao, despesa, outra, cartoesDoUsuario: []).Error;

        ErrosParaModelo.De(erro).Should().BeSameAs(ErrosParaModelo.Vinculo.TransacaoJaVinculada);
    }

    [Fact]
    public void De_Quando_ErroSemMensagemMapeada_Deve_LancarExcecao()
    {
        Action act = () => ErrosParaModelo.De(ErroTransacaoNaoCobrouADespesa.Instancia);

        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
