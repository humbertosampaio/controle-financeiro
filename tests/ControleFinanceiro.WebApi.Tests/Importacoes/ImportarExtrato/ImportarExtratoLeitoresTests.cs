using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// Cada endpoint escolhe o leitor pelo layout da rota: o validator aceita os layouts do seu tipo (extrato ou fatura), e
/// cada subgrupo do tipo tem o seu leitor: o extrato com a conta no arquivo e a fatura com o vencimento no arquivo vão ao
/// leitor que só recebe o arquivo, o extrato de conta informada, ao que recebe o sufixo, e a fatura de vencimento
/// informado, ao que recebe o vencimento. Um layout sem leitor no container da API daria 500.
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
    public void Api_Deve_RegistrarUmLeitorDeExtratoParaCadaLayoutComContaNoArquivo()
    {
        IReadOnlyList<ILeitorExtrato> leitores = _api.ObterServicos<ILeitorExtrato>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.ExtratosContaNoArquivo);
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeContaInformadaParaCadaLayoutDeContaInformada()
    {
        IReadOnlyList<ILeitorExtratoContaInformada> leitores = _api.ObterServicos<ILeitorExtratoContaInformada>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.ExtratosContaInformada);
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeFaturaParaCadaLayoutComVencimentoNoArquivo()
    {
        IReadOnlyList<ILeitorFatura> leitores = _api.ObterServicos<ILeitorFatura>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.FaturasVencimentoNoArquivo);
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorDeVencimentoInformadoParaCadaLayoutDeVencimentoInformado()
    {
        IReadOnlyList<ILeitorFaturaVencimentoInformado> leitores = _api.ObterServicos<ILeitorFaturaVencimentoInformado>();

        leitores.Select(l => l.Layout).Should().BeEquivalentTo(LayoutArquivoImportacao.FaturasVencimentoInformado);
    }
}
