using AutoFixture;
using ControleFinanceiro.Mcp.Tests.Infraestrutura;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.Mcp.Tests.Ferramentas;

/// <summary>Os candidatos a pagamento de fatura pelo processo real, como o modelo os recebe para ligar a fatura.</summary>
[Collection(ServidorMcpCollection.Nome)]
public sealed class ListarCandidatosPagamentoFaturaToolTests : IAsyncLifetime
{
    private const string NomeFerramenta = "listar_candidatos_pagamento_fatura";

    private readonly ServidorMcpFixture _servidor;
    private readonly PagamentoFaturaDeTeste _fatura;

    public ListarCandidatosPagamentoFaturaToolTests(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
        _fatura = new PagamentoFaturaDeTeste(servidor);
    }

    public Task InitializeAsync() => _servidor.ResetarBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Anotacoes_Deve_SerDeLeituraIdempotenteEFechada()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ToolAnnotations anotacoes = ferramenta.ProtocolTool.Annotations!;
        anotacoes.ReadOnlyHint.Should().BeTrue();
        anotacoes.IdempotentHint.Should().BeTrue();
        anotacoes.OpenWorldHint.Should().BeFalse();
    }

    [Fact]
    public async Task Esquema_Deve_PedirOAnoEOMes()
    {
        McpClientTool ferramenta = await _servidor.ClienteMcp.ObterFerramentaAsync(NomeFerramenta);

        ferramenta.ProtocolTool.InputSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("ano", "mes");
    }

    [Theory, DomainAutoData]
    public async Task Listar_Quando_SaidaComOTotalNoVencimento_Deve_DevolverOCandidato(Fixture fixture)
    {
        CandidatoPagamentoFaturaGravado candidatoGravado = await _fatura.GravarCandidatoPagamentoFaturaAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.ListarCandidatosPagamentoFaturaAsync(candidatoGravado.Vencimento.Mes);

        CandidatosPagamentoFaturaLidos lidos = new(resultado.Json);
        lidos.Mes.Should().Be(candidatoGravado.Vencimento.Mes.ToString());
        CandidatoPagamentoFaturaLido lido = lidos.Candidatos.Should().ContainSingle().Subject;
        lido.Transacao.Id.Should().Be(candidatoGravado.Saida.Id.Value);
        lido.IdCartao.Should().Be(candidatoGravado.Cartao.Id.Value);
        lido.Banco.Should().Be(candidatoGravado.Cartao.Banco.Name);
        lido.Vencimento.Should().Be(candidatoGravado.Vencimento);
        lido.Total.Should().Be(100m);
    }

    // O final do cartão fica de fora: o prever_pagamento_fatura pede só o id de um cartão da fatura.
    [Theory, DomainAutoData]
    public async Task Listar_Deve_NaoMandarOFinalDoCartao(Fixture fixture)
    {
        CandidatoPagamentoFaturaGravado candidatoGravado = await _fatura.GravarCandidatoPagamentoFaturaAsync(fixture);

        CallToolResult resultado = await _servidor.ClienteMcp.ListarCandidatosPagamentoFaturaAsync(candidatoGravado.Vencimento.Mes);

        JsonObject candidato = resultado.Json["candidatos"]!.AsArray().Single()!.AsObject();
        candidato.Select(campo => campo.Key).Should().NotContain("final");
        candidato["transacao"]!.AsObject().Select(campo => campo.Key).Should().NotContain("final");
    }

    [Fact]
    public async Task Listar_Quando_MesInvalido_Deve_DevolverApenasOErroSemRepetirOMes()
    {
        CallToolResult resultado = await _servidor.ClienteMcp.ChamarDoMesAsync(NomeFerramenta, ano: 2026, mes: 13);

        resultado.IsError.Should().NotBe(true);
        resultado.Json.Select(campo => campo.Key).Should().Equal("erro");
        resultado.Texto.Should().NotContain("13");
    }
}
