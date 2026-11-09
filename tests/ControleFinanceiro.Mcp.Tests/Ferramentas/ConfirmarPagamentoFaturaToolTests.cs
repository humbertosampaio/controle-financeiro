using AutoFixture;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// A confirmação do pagamento da fatura pelo processo real, chamada como o cliente a envia depois que o usuário aprova:
/// grava só quando o resumo é o da prévia e nada mudou entre as duas etapas.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ConfirmarPagamentoFaturaToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "confirmar_pagamento_fatura";

    private readonly ServidorMcpFixture _servidor;
    private readonly PagamentoFaturaDeTeste _fatura;

    public ConfirmarPagamentoFaturaToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _fatura = new PagamentoFaturaDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O resumo vem primeiro, para ser o que o usuário lê primeiro no pedido de aprovação.
    [Fact]
    public async Task Esquema_Deve_TerOResumoOsIdsEOMesDeVencimento()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().Equal("resumo", "idTransacao", "idCartao", "ano", "mes");
    }

    [Fact]
    public async Task Meta_Deve_PedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.Meta![ServidorMcpFixture.MetaInteracaoDoUsuario]!.GetValue<bool>().Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_ResumoDaPrevia_Deve_GravarEDevolverOPagamento(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);
        string resumo = await PreverAsync(saida, cartao, adiada.MesReferencia);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia, resumo);

        PagamentoFaturaLido pagamento = new(resultado.Json);
        pagamento.Despesas.Should().ContainSingle().Which.Id.Should().Be(adiada.Id.Value);
        pagamento.Resumo.Should().Be(resumo);
        await _fatura.DeveEstarPagaPelaSaidaAsync(adiada, saida);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_ResumoAlterado_Deve_DevolverResumoDivergenteSemGravar(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);
        string resumo = await PreverAsync(saida, cartao, adiada.MesReferencia);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(
            saida.Id.Value,
            cartao.Id.Value,
            adiada.MesReferencia,
            resumo.Replace("1 despesa", "2 despesas", StringComparison.Ordinal));

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.ResumoPagamentoFaturaDivergente.Mensagem);
        await _fatura.DeveEstarPendenteAsync(adiada);
    }

    // O usuário aprovou o total da prévia: uma cobrança que chegou depois muda o total, e nada é gravado.
    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_CobrancaChegaDepoisDaPrevia_Deve_DevolverResumoDivergenteSemGravar(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);
        string resumo = await PreverAsync(saida, cartao, adiada.MesReferencia);
        DespesaAdiada nova = await _fatura.GravarAdiadaCobradaAsync(fixture, cartao);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.ResumoPagamentoFaturaDivergente.Mensagem);
        await _fatura.DeveEstarPendenteAsync(adiada);
        await _fatura.DeveEstarPendenteAsync(nova);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_DuasVezes_Deve_DevolverFaturaSemCobrancaPendente(Fixture fixture)
    {
        (Cartao cartao, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);
        string resumo = await PreverAsync(saida, cartao, adiada.MesReferencia);
        await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia, resumo);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, adiada.MesReferencia, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.FaturaSemCobrancaPendente.Mensagem);
        await _fatura.DeveEstarPagaPelaSaidaAsync(adiada, saida);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_CartaoNaoExiste_Deve_DevolverCartaoNaoEncontradoSemGravar(Fixture fixture, IdCartao inexistente, string resumo)
    {
        (_, DespesaAdiada adiada, Transacao saida) = await _fatura.GravarAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarPagamentoFaturaAsync(saida.Id.Value, inexistente.Value, adiada.MesReferencia, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.CartaoNaoEncontrado.Mensagem);
        await _fatura.DeveEstarPendenteAsync(adiada);
    }

    private async Task<string> PreverAsync(Transacao saida, Cartao cartao, MesReferencia mesVencimento)
    {
        CallToolResult previa = await _servidor.ClienteMcp.PreverPagamentoFaturaAsync(saida.Id.Value, cartao.Id.Value, mesVencimento);
        return new PagamentoFaturaLido(previa.Json).Resumo;
    }
}
