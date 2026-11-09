using AutoFixture;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>A prévia do pagamento da fatura pelo processo real: devolve as despesas pagas e o resumo, e nunca grava.</summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class PreverPagamentoFaturaToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "prever_pagamento_fatura";

    private readonly ServidorMcpFixture _servidor;
    private readonly PagamentoFaturaDeTeste _fatura;

    public PreverPagamentoFaturaToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _fatura = new PagamentoFaturaDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O serviço é ligado pelo SDK: aparecendo aqui, o modelo teria de preenchê-lo.
    [Fact]
    public async Task Esquema_Deve_TerOsIdsEOMesDeVencimento()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().BeEquivalentTo("idTransacao", "idCartao", "ano", "mes");
    }

    [Fact]
    public async Task Meta_Deve_NaoPedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        (ferramenta.ProtocolTool.Meta?.ContainsKey(ServidorMcpFixture.MetaInteracaoDoUsuario) ?? false).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public async Task Prever_Deve_DevolverAsDespesasPagasEOResumo(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia);

        PagamentoFaturaLido pagamento = new(resultado.Json);
        pagamento.Transacao.Id.Should().Be(saida.Id.Value);
        DespesaDoMesLida paga = pagamento.Despesas.Should().ContainSingle().Subject;
        paga.Id.Should().Be(adiada.Id.Value);
        paga.Status.Should().Be(StatusPagamento.Pago.Name);
        pagamento.Total.Should().Be(adiada.Cobranca!.Valor);
        pagamento.Resumo.Should().Contain(cartao.Banco.Name).And.NotContain(saida.Id.Value.ToString());
    }

    [Theory, DomainAutoData]
    public async Task Prever_Deve_NaoGravar(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia);

        await _fatura.DeveEstarPendenteAsync(adiada);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_OutroMes_Deve_DevolverFaturaSemCobrancaPendente(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia.Posterior());

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.FaturaSemCobrancaPendente.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_MesInvalido_Deve_DevolverMesInvalido(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.CallToolAsync(
            NomeFerramenta,
            new Dictionary<string, object?>
            {
                ["idTransacao"] = saida.Id.Value,
                ["idCartao"] = cartao.Id.Value,
                ["ano"] = adiada.MesReferencia.Ano,
                ["mes"] = 13,
            });

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.MesInvalido.Mensagem);
    }

    // A saída que pagou a fatura de um mês não paga a do mês seguinte.
    [Theory, DomainAutoData]
    public async Task Prever_Quando_SaidaPagouOutraFatura_Deve_DevolverTransacaoPagaOutraFatura(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);
        await _fatura.PagarAsync(saida, cartao, adiada.MesReferencia);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia.Posterior());

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoPagaOutraFatura.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_CartaoNaoExiste_Deve_DevolverCartaoNaoEncontrado(Fixture fixture, IdCartao inexistente)
    {
        (_, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, inexistente.Value, adiada.MesReferencia);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.CartaoNaoEncontrado.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_TransacaoNaoExiste_Deve_DevolverTransacaoNaoEncontrada(Fixture fixture, IdTransacao inexistente)
    {
        (Cartao cartao, DespesaAdiada adiada, _) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(inexistente.Value, cartao.Id.Value, adiada.MesReferencia);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoNaoEncontrada.Mensagem);
    }

    // A compra do cartão não paga a fatura: só a saída da conta.
    [Theory, DomainAutoData]
    public async Task Prever_Quando_CompraDoCartao_Deve_DevolverOrigemIncompativel(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, _) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(adiada.Cobranca!.IdTransacao.Value, cartao.Id.Value, adiada.MesReferencia);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.OrigemIncompativel.Mensagem);
    }
}
