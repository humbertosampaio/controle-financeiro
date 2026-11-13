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
using CSharpFunctionalExtensions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// A confirmação do vínculo pelo processo real, chamada como o cliente a envia depois que o usuário aprova: grava só
/// quando o resumo é o da prévia e nada mudou entre as duas etapas.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ConfirmarVinculoToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "confirmar_vinculo";

    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;
    private readonly VinculoDeTeste _vinculo;

    public ConfirmarVinculoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
        _vinculo = new VinculoDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // O resumo vem primeiro, para ser o que o usuário lê primeiro no pedido de aprovação.
    [Fact]
    public async Task Esquema_Deve_TerOResumoEOsDoisIds()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().Equal("resumo", "idTransacao", "idDespesa");
    }

    [Fact]
    public async Task Meta_Deve_PedirAInteracaoDoUsuario()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.Meta![ServidorMcpFixture.MetaInteracaoDoUsuario]!.GetValue<bool>().Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_ResumoDaPrevia_Deve_GravarEDevolverOVinculo(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        string resumo = await PreverAsync(transacao, despesa);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);

        VinculoLido vinculo = new(resultado.Json);
        vinculo.Transacao.Id.Should().Be(transacao.Id.Value);
        vinculo.Despesa.Id.Should().Be(despesa.Id.Value);
        vinculo.Resumo.Should().Be(resumo);
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentosRealizadosPor(transacao.Id).Should().ContainSingle().Which.MesReferencia.Should().Be(mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_ResumoAlterado_Deve_DevolverResumoDivergenteSemGravar(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        string resumo = await PreverAsync(transacao, despesa);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(
            transacao.Id.Value,
            despesa.Id.Value,
            resumo.Replace("despesa", "despesas", StringComparison.Ordinal));

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.ResumoDivergente.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
    }

    // O resumo de uma despesa não serve para confirmar outra, mesmo que o usuário tenha aprovado o texto.
    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_ResumoDeOutraDespesa_Deve_DevolverResumoDivergenteSemGravar(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        Despesa outra = await _vinculo.GravarDespesaAsync(fixture, mes);
        string resumoDaOutra = await PreverAsync(transacao, outra);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumoDaOutra);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.ResumoDivergente.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
        await _vinculo.DeveEstarPendenteAsync(outra, mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_DespesaPagaPorOutraSaidaDepoisDaPrevia_Deve_DevolverDespesaJaPagaSemGravar(
        MesReferencia mes,
        Fixture fixture)
    {
        Despesa despesa = await _vinculo.GravarDespesaAsync(fixture, mes);
        IReadOnlyList<Transacao> saidas = await _vinculo.GravarExtratoAsync(
            fixture,
            fixture.CreateMany<Lancamento>(2).Select(lancamento => lancamento.SemCategoria().ComoSaida()).ToArray());
        (Transacao transacao, Transacao outraSaida) = (saidas[0], saidas[1]);
        string resumo = await PreverAsync(transacao, despesa);
        await GravarVinculoAsync(outraSaida, despesa);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.DespesaJaPaga.Mensagem);
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentosRealizadosPor(transacao.Id).Should().BeEmpty();
        gravada.PagamentosRealizadosPor(outraSaida.Id).Should().ContainSingle().Which.MesReferencia.Should().Be(mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_TransacaoVinculadaAOutraDespesaDepoisDaPrevia_Deve_DevolverTransacaoJaVinculadaSemGravar(
        MesReferencia mes,
        Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        Despesa outra = await _vinculo.GravarDespesaAsync(fixture, mes);
        string resumo = await PreverAsync(transacao, despesa);
        await GravarVinculoAsync(transacao, outra);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.TransacaoJaVinculada.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_DuasVezes_Deve_DevolverTransacaoJaVinculada(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        string resumo = await PreverAsync(transacao, despesa);
        await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.TransacaoJaVinculada.Mensagem);
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.PagamentosRealizadosPor(transacao.Id).Should().ContainSingle().Which.MesReferencia.Should().Be(mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_TransacaoNaoExiste_Deve_DevolverTransacaoNaoEncontrada(
        MesReferencia mes,
        Fixture fixture,
        IdTransacao inexistente,
        string resumo)
    {
        Despesa despesa = await _vinculo.GravarDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(inexistente.Value, despesa.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Consulta.TransacaoNaoEncontrada.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_DespesaNaoExiste_Deve_DevolverDespesaNaoEncontrada(
        MesReferencia mes,
        Fixture fixture,
        IdDespesa inexistente,
        string resumo)
    {
        (Transacao transacao, _) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, inexistente.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Consulta.DespesaNaoEncontrada.Mensagem);
    }

    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_DespesaDeOutroUsuario_Deve_DevolverDespesaNaoEncontradaSemGravar(
        Usuario outroUsuario,
        MesReferencia mes,
        Fixture fixture,
        string resumo)
    {
        (Transacao transacao, _) = await _vinculo.GravarSaidaEDespesaAsync(fixture, mes);
        Despesa deOutroUsuario = await _vinculo.GravarDespesaDeOutroUsuarioAsync(fixture, outroUsuario, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, deOutroUsuario.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Consulta.DespesaNaoEncontrada.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(deOutroUsuario, mes);
    }

    // O lançamento gerado tem valor positivo: é uma entrada.
    [Theory, DomainAutoData]
    public async Task Confirmar_Quando_TransacaoDeEntrada_Deve_DevolverErroDaRegraSemGravar(MesReferencia mes, Fixture fixture, string resumo)
    {
        Despesa despesa = await _vinculo.GravarDespesaAsync(fixture, mes);
        Transacao entrada = await _vinculo.GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria());

        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(entrada.Id.Value, despesa.Id.Value, resumo);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErrosParaModelo.Vinculo.TransacaoNaoEhSaida.Mensagem);
        await _vinculo.DeveEstarPendenteAsync(despesa, mes);
    }


    private async Task<string> PreverAsync(Transacao transacao, Despesa despesa)
    {
        CallToolResult previa = await _servidor.ClienteMcp.PreverVinculoAsync(transacao.Id.Value, despesa.Id.Value);
        return new VinculoLido(previa.Json).Resumo;
    }

    /// <summary>Grava um vínculo pelas duas etapas, como outra conversa faria entre a prévia e a confirmação do teste.</summary>
    private async Task GravarVinculoAsync(Transacao transacao, Despesa despesa)
    {
        string resumo = await PreverAsync(transacao, despesa);
        CallToolResult resultado = await _servidor.ClienteMcp.ConfirmarVinculoAsync(transacao.Id.Value, despesa.Id.Value, resumo);
        resultado.Json.Should().NotContainKey("erro");
    }
}
