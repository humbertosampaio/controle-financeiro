using AutoFixture;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>A prévia do vínculo pelo processo real: devolve o vínculo e o resumo, e nunca grava.</summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class PreverVinculoToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "prever_vinculo";

    private readonly ServidorMcpFixture _servidor;
    private readonly VinculoDeTeste _vinculo;

    public PreverVinculoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _vinculo = new VinculoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O serviço é ligado pelo SDK: aparecendo aqui, o modelo teria de preenchê-lo.
    [Fact]
    public async Task Esquema_Deve_TerSoOsDoisIds()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().BeEquivalentTo("idTransacao", "idDespesa");
    }

    // Só a ferramenta que grava pede a aprovação do usuário: a prévia corre sem interrupção.
    [Fact]
    public async Task Meta_Deve_NaoPedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        (ferramenta.ProtocolTool.Meta?.ContainsKey(ServidorMcpFixture.MetaInteracaoDoUsuario) ?? false).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public async Task Prever_Deve_DevolverOVinculoComOResumo(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, despesa.Id.Value);

        VinculoLido vinculo = new(resultado.Json);
        vinculo.Transacao.Id.Should().Be(transacao.Id.Value);
        vinculo.Despesa.Id.Should().Be(despesa.Id.Value);
        vinculo.Despesa.Status.Should().Be(StatusPagamento.Pago.Name);
        vinculo.ValorProjetado.Should().Be(despesa.PagamentoDoMes(mes).Value.ValorProjetado);
        vinculo.Resumo.Should().Contain(despesa.Descricao).And.NotContain(transacao.Id.Value.ToString());
    }

    [Theory, DomainAutoData]
    public async Task Prever_Deve_NaoGravar(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);

        await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, despesa.Id.Value);

        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_TransacaoNaoExiste_Deve_DevolverTransacaoNaoEncontrada(
        MesReferencia mes,
        Fixture fixture,
        IdTransacao inexistente)
    {
        Despesa despesa = await _vinculo.GravarDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(inexistente.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoNaoEncontrada.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_DespesaNaoExiste_Deve_DevolverDespesaNaoEncontrada(MesReferencia mes, Fixture fixture, IdDespesa inexistente)
    {
        (Transacao transacao, _) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, inexistente.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.DespesaNaoEncontrada.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Prever_Quando_DespesaDeOutroUsuario_Deve_DevolverDespesaNaoEncontrada(
        Usuario outroUsuario,
        MesReferencia mes,
        Fixture fixture)
    {
        (Transacao transacao, _) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        Despesa deOutroUsuario = await _vinculo.GravarDespesaDeOutroUsuarioAsync(fixture, outroUsuario, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, deOutroUsuario.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.DespesaNaoEncontrada.Mensagem);
    }

    // O lançamento gerado tem valor positivo: é uma entrada.
    [Theory, DomainAutoData]
    public async Task Prever_Quando_TransacaoDeEntrada_Deve_DevolverErroDaRegra(MesReferencia mes, Fixture fixture)
    {
        Despesa despesa = await _vinculo.GravarDespesaAsync(fixture, mes);
        Transacao entrada = await _vinculo.GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria());

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(entrada.Id.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoNaoEhSaida.Mensagem);
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public async Task Prever_Deve_DevolverOResumoSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        MesReferencia mes,
        Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)$"LOJA {documentos.Em(forma)}");
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, despesa.Id.Value);

        new VinculoLido(resultado.Json).Resumo
            .Should().NotContain(documentos.Em(forma))
            .And.Contain($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }
}
