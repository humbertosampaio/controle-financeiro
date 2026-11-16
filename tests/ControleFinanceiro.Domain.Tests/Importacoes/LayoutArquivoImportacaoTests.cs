using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

/// <summary>
/// Os grupos do layout dividem os casos sem sobra nem repetição: quem escolhe o comportamento por um grupo encontra
/// todo layout do seu tipo em exatamente um subgrupo.
/// </summary>
public sealed class LayoutArquivoImportacaoTests
{
    [Fact]
    public void Extratos_Deve_SerAUniaoSemRepeticaoDosExtratosComAContaNoArquivoEInformada()
    {
        LayoutArquivoImportacao.ExtratosContaNoArquivo.Intersect(LayoutArquivoImportacao.ExtratosContaInformada).Should().BeEmpty();
        LayoutArquivoImportacao.ExtratosContaNoArquivo
            .Concat(LayoutArquivoImportacao.ExtratosContaInformada)
            .Should().BeEquivalentTo(LayoutArquivoImportacao.Extratos);
    }

    [Fact]
    public void Faturas_Deve_SerAUniaoSemRepeticaoDasFaturasComOVencimentoNoArquivoEInformado()
    {
        LayoutArquivoImportacao.FaturasVencimentoNoArquivo.Intersect(LayoutArquivoImportacao.FaturasVencimentoInformado).Should().BeEmpty();
        LayoutArquivoImportacao.FaturasVencimentoNoArquivo
            .Concat(LayoutArquivoImportacao.FaturasVencimentoInformado)
            .Should().BeEquivalentTo(LayoutArquivoImportacao.Faturas);
    }

    [Fact]
    public void List_Deve_SerAUniaoSemRepeticaoDosExtratosEDasFaturas()
    {
        LayoutArquivoImportacao.Extratos.Intersect(LayoutArquivoImportacao.Faturas).Should().BeEmpty();
        LayoutArquivoImportacao.Extratos
            .Concat(LayoutArquivoImportacao.Faturas)
            .Should().BeEquivalentTo(LayoutArquivoImportacao.List);
    }
}
