using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Cada endpoint escolhe o leitor pelo layout, e o validator aceita os layouts do seu tipo (extrato ou fatura). Um
/// layout sem leitor no container da API passaria pelo validator e daria 500.
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
    public void Api_Deve_RegistrarUmLeitorDeFaturaParaCadaLayoutDeFatura()
    {
        IReadOnlyList<ILeitorFatura> leitores = _api.ObterServicos<ILeitorFatura>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.Faturas);
    }
}
