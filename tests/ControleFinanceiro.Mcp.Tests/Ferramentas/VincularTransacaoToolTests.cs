using System.Text.Json;
using AutoFixture;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>
/// O vínculo pelo processo real: a despesa é Pix, paga por uma saída da conta, e o usuário responde pela
/// <see cref="ServidorMcpFixture.Elicitacao"/>. Gravado ou não, o que vale é o que se lê de volta do banco.
/// </summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class VincularTransacaoToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "vincular_transacao";

    private readonly ServidorMcpFixture _servidor;
    private readonly BancoDeTeste _banco;

    public VincularTransacaoToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _banco = new BancoDeTeste(servidor);
    }

    public async Task InitializeAsync()
    {
        _servidor.ReiniciarElicitacao();
        await _servidor.ResetarBancoAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // O McpServer, o serviço e o logger são ligados pelo SDK: aparecendo aqui, o modelo teria de preenchê-los.
    [Fact]
    public async Task Esquema_Deve_TerSoOsDoisIds()
    {
        IList<McpClientTool> ferramentas = await _servidor.ClienteMcp.ListToolsAsync();

        JsonElement esquema = ferramentas.Single(f => f.Name == NomeFerramenta).ProtocolTool.InputSchema;

        esquema
            .GetProperty("properties")
            .EnumerateObject()
            .Select(p => p.Name)
            .Should().BeEquivalentTo("idTransacao", "idDespesa");
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_Confirmado_Deve_GravarEDevolverOVinculo(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);
        _servidor.ProgramarElicitacao(RespostaElicitacao.Confirmar());

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        VinculoLido vinculo = new(resultado.Json);
        vinculo.Transacao.Id.Should().Be(transacao.Id.Value);
        vinculo.Despesa.Id.Should().Be(despesa.Id.Value);
        vinculo.Despesa.Status.Should().Be(StatusPagamento.Pago.Name);
        vinculo.ValorProjetado.Should().Be(despesa.PagamentoDoMes(mes).Value.ValorProjetado);
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.MesQuitado(transacao.Id).Should().HaveValue(mes);
    }

    [Theory]
    [InlineDomainAutoData("decline", false)]
    [InlineDomainAutoData("cancel", false)]
    [InlineDomainAutoData("accept", false)]
    public async Task Vincular_Quando_NaoConfirmado_Deve_NaoGravar(string acao, bool confirmar, MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);
        _servidor.ProgramarElicitacao(RespostaElicitacao.Com(acao, confirmar));

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.ConfirmacaoRecusada.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().ContainSingle();
        await DeveEstarPendenteAsync(despesa, transacao, mes);
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_ClienteSemElicitation_Deve_NaoGravar(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteMcp.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.ConfirmacaoIndisponivel.Mensagem);
        await DeveEstarPendenteAsync(despesa, transacao, mes);
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_ElicitationFalha_Deve_NaoGravar(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);
        _servidor.ProgramarFalhaDaElicitacao();
        await using McpClient clienteAntigo = await ServidorMcpFixture.IniciarClienteMcpAsync(
            _servidor.ConexaoDoContainer,
            _servidor.Usuario.Id,
            new McpClientOptions
            {
                ProtocolVersion = "2025-11-25",
                Handlers = new McpClientHandlers { ElicitationHandler = _servidor.Elicitacao },
            });

        CallToolResult resultado = await clienteAntigo.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.ConfirmacaoIndisponivel.Mensagem);
        await DeveEstarPendenteAsync(despesa, transacao, mes);
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_TransacaoNaoExiste_Deve_DevolverErroSemPerguntar(MesReferencia mes, Fixture fixture, IdTransacao inexistente)
    {
        (_, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(inexistente.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoNaoEncontrada.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_DespesaNaoExiste_Deve_DevolverErroSemPerguntar(MesReferencia mes, Fixture fixture, IdDespesa inexistente)
    {
        (Transacao transacao, _) = await GravarSaidaEDespesaAsync(fixture, mes);

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, inexistente.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.DespesaNaoEncontrada.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_DespesaDeOutroUsuario_Deve_DevolverDespesaNaoEncontrada(
        Usuario outroUsuario,
        MesReferencia mes,
        Fixture fixture)
    {
        (Transacao transacao, _) = await GravarSaidaEDespesaAsync(fixture, mes);
        await _banco.GravarUsuarioAsync(outroUsuario);
        fixture.Inject(outroUsuario.Id);
        Despesa deOutroUsuario = await _banco.GravarDespesaDoMesAsync(await _banco.GravarRecorrenteAsync(fixture), mes);

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, deOutroUsuario.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.DespesaNaoEncontrada.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().BeEmpty();
    }

    // O lançamento gerado tem valor positivo: é uma entrada.
    [Theory, DomainAutoData]
    public async Task Vincular_Quando_TransacaoDeEntrada_Deve_DevolverErroDaRegraSemPerguntar(MesReferencia mes, Fixture fixture)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Despesa despesa = await _banco.GravarDespesaDoMesAsync(await _banco.GravarRecorrenteAsync(fixture), mes);
        Transacao entrada = await GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria());

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(entrada.Id.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoNaoEhSaida.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public async Task Vincular_Quando_JaVinculada_Deve_DevolverErroSemPerguntarDeNovo(MesReferencia mes, Fixture fixture)
    {
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);
        _servidor.ProgramarElicitacao(RespostaElicitacao.Confirmar());
        await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        CallToolResult resultado = await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        ErroLido.DoEnvelope(resultado.Json).Mensagem.Should().Be(ErroParaModelo.TransacaoJaVinculada.Mensagem);
        _servidor.Elicitacao.ReceivedCalls().Should().ContainSingle();
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.MesQuitado(transacao.Id).Should().HaveValue(mes);
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public async Task Vincular_Deve_MostrarAoUsuarioOsTextosSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        MesReferencia mes,
        Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)$"LOJA {documentos.Em(forma)}");
        (Transacao transacao, Despesa despesa) = await GravarSaidaEDespesaAsync(fixture, mes);

        await _servidor.ClienteComConfirmacao.VincularAsync(transacao.Id.Value, despesa.Id.Value);

        ElicitRequestParams pedido = (ElicitRequestParams)_servidor.Elicitacao.ReceivedCalls().Single().GetArguments()[0]!;
        pedido.Message.Should().NotContain(documentos.Em(forma)).And.Contain($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    /// <summary>Uma despesa Pix do usuário do servidor, pendente no mês, e uma saída da conta dele que pode pagá-la.</summary>
    private async Task<(Transacao Transacao, Despesa Despesa)> GravarSaidaEDespesaAsync(IFixture fixture, MesReferencia mes)
    {
        fixture.Inject(_servidor.Usuario.Id);
        Despesa despesa = await _banco.GravarDespesaDoMesAsync(await _banco.GravarRecorrenteAsync(fixture), mes);
        Transacao saida = await GravarExtratoAsync(fixture, fixture.Create<Lancamento>().SemCategoria().ComoSaida());
        return (saida, despesa);
    }

    /// <summary>Grava o lançamento como um extrato de uma conta nova do usuário injetado no <paramref name="fixture"/>.</summary>
    private async Task<Transacao> GravarExtratoAsync(IFixture fixture, Lancamento lancamento)
    {
        ContaBancaria conta = fixture.Freeze<ContaBancaria>();

        // O Postgres só grava o instante em UTC, como o TimeProvider da aplicação o dá.
        fixture.Inject(fixture.Create<DateTimeOffset>().ToUniversalTime());

        return (await _banco.GravarExtratoAsync(conta, fixture.Create<ImportacaoArquivo>(), [lancamento])).Single();
    }

    private async Task DeveEstarPendenteAsync(Despesa despesa, Transacao transacao, MesReferencia mes)
    {
        Despesa gravada = await _banco.LerDespesaAsync(despesa);
        gravada.MesQuitado(transacao.Id).Should().HaveNoValue();
        gravada.PagamentoDoMes(mes).Value.Status.Should().Be(StatusPagamento.Pendente);
    }
}
