using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class LayoutArquivoImportacaoTests
{
    [Fact]
    public void Listas_Deve_TrazerCadaLayoutEmUmaListaSo()
    {
        LayoutArquivoImportacao.Extratos
            .Concat(LayoutArquivoImportacao.ExtratosContaInformada)
            .Concat(LayoutArquivoImportacao.Faturas)
            .Should().BeEquivalentTo(LayoutArquivoImportacao.List);
    }
}
