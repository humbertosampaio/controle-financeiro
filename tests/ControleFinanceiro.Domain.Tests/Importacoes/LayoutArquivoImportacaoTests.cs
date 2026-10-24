using ControleFinanceiro.Domain.Importacoes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class LayoutArquivoImportacaoTests
{
    [Fact]
    public void ExtratosEFaturas_Deve_TrazerCadaLayoutEmUmaListaSo()
    {
        LayoutArquivoImportacao.Extratos.Concat(LayoutArquivoImportacao.Faturas)
            .Should().BeEquivalentTo(LayoutArquivoImportacao.List);
    }
}
