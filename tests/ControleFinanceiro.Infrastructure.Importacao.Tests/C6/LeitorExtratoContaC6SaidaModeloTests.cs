using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Infrastructure.Importacao.Tests.C6;

/// <summary>
/// Segue a linha do Pix enviado de <c>Fixtures/C6/extrato-conta.csv</c>, com os textos trocados, do leitor até o que sai
/// para o modelo. Os documentos são aleatórios, e nada vem de <c>dados/</c>.
/// </summary>
public sealed class LeitorExtratoContaC6SaidaModeloTests
{
    private const int LinhaPixEnviado = 11;
    private const int IndiceLancamentoPixEnviado = 1;

    private static readonly ArquivoFixture _fixture = new("C6", "extrato-conta.csv", fimLinha: "\r\n", comBom: true);

    private readonly LeitorExtratoContaC6 _leitor = new();

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfPuro)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public async Task ParaModelo_Quando_ContraparteNoTituloComDocumento_Deve_SairSemODocumento(FormaDocumento forma, DocumentosDeTeste documentos, TransacoesDeTeste transacoes)
    {
        TransacaoParaModelo saida = await LerAsync(transacoes, $"Pix enviado para LOJA FICTICIA {documentos.Em(forma)}", descricao: string.Empty);

        saida.Descricao.Should().Be("Pix enviado para");
        saida.Contraparte.Should().Be($"LOJA FICTICIA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjPuro)]
    public async Task ParaModelo_Quando_TituloSoComDocumentoDepoisDoPrefixo_Deve_OmitirAContraparte(FormaDocumento forma, DocumentosDeTeste documentos, TransacoesDeTeste transacoes)
    {
        TransacaoParaModelo saida = await LerAsync(transacoes, $"Pix enviado para {documentos.Em(forma)}", descricao: string.Empty);

        saida.Contraparte.Should().BeNull();
    }

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComLetras)]
    public async Task ParaModelo_Quando_ColunaDescricaoComDocumento_Deve_SairSemODocumento(FormaDocumento forma, DocumentosDeTeste documentos, TransacoesDeTeste transacoes)
    {
        TransacaoParaModelo saida = await LerAsync(transacoes, "Pix enviado para LOJA FICTICIA", $"Aluguel {documentos.Em(forma)}");

        saida.Descricao.Should().Be($"Pix enviado para · Aluguel {DocumentosDeTeste.Rotulo(forma)}");
    }

    /// <summary>O rótulo solto no fim é um limite aceito: o texto de antes dele ainda tem conteúdo.</summary>
    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfPuro)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjComPontuacao)]
    public async Task ParaModelo_Quando_ColunaDescricaoSoComDocumento_Deve_SairSoORotulo(FormaDocumento forma, DocumentosDeTeste documentos, TransacoesDeTeste transacoes)
    {
        TransacaoParaModelo saida = await LerAsync(transacoes, "Pix enviado para LOJA FICTICIA", documentos.Em(forma));

        saida.Descricao.Should().Be($"Pix enviado para · {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineUsuarioUnicoAutoData(FormaDocumento.CnpjPuro)]
    public async Task ParaModelo_Quando_TituloSemPrefixoDePix_Deve_SairSemODocumento(FormaDocumento forma, DocumentosDeTeste documentos, TransacoesDeTeste transacoes)
    {
        TransacaoParaModelo saida = await LerAsync(transacoes, $"Pagamento LOJA FICTICIA {documentos.Em(forma)}", descricao: string.Empty);

        saida.Descricao.Should().Be($"Pagamento LOJA FICTICIA {DocumentosDeTeste.Rotulo(forma)}");
        saida.Contraparte.Should().BeNull();
    }

    private async Task<TransacaoParaModelo> LerAsync(TransacoesDeTeste transacoes, string titulo, string descricao)
    {
        Stream arquivo = _fixture.ComLinha(LinhaPixEnviado, $"02/03/2026,02/03/2026,{titulo},{descricao},0.00,89.90,-12.50");
        Result<Extrato> extrato = await _leitor.LerAsync(arquivo, CancellationToken.None);
        Lancamento lancamento = extrato.Should().Succeed().Which.Lancamentos[IndiceLancamentoPixEnviado];
        return transacoes.DaConta(lancamento).ParaModelo();
    }
}
