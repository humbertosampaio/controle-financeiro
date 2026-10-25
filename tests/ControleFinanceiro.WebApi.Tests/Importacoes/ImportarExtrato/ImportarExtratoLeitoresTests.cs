using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Cada endpoint escolhe o leitor pelo layout: o validator aceita os layouts do seu tipo (extrato ou fatura), e a rota de
/// um extrato de conta informada fixa o seu. Um layout sem leitor no container da API daria 500.
/// </summary>
[Collection(ApiCollection.Nome)]
public sealed class ImportarExtratoLeitoresTests
{
    private readonly ApiComPostgresFixture _api;

    public ImportarExtratoLeitoresTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeExtratoParaCadaLayoutDeExtrato()
    {
        IReadOnlyList<ILeitorExtrato> leitores = _api.ObterServicos<ILeitorExtrato>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.Extratos);
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeContaInformadaParaCadaLayoutDeContaInformada()
    {
        IReadOnlyList<ILeitorExtratoContaInformada> leitores = _api.ObterServicos<ILeitorExtratoContaInformada>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.ExtratosContaInformada);
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeFaturaParaCadaLayoutDeFatura()
    {
        IReadOnlyList<ILeitorFatura> leitores = _api.ObterServicos<ILeitorFatura>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.Faturas);
    }
}
