using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;

/// <summary>
/// Monta um <see cref="ImportarFaturaRequest"/> válido para os testes do validator, com o layout da fatura, o dia
/// único, o dia de um final e um arquivo de 1 byte: o validator só confere o tamanho, e o conteúdo é do leitor. O
/// teste do endpoint manda o formulário multipart direto, com a fatura fictícia do C6.
/// </summary>
internal sealed class ImportarFaturaRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Guid idUsuario, DiaVencimentoCartao diaUnico, SufixoCartao sufixo, DiaVencimentoCartao diaDoFinal) =>
            new ImportarFaturaRequest
            {
                IdUsuario = idUsuario,
                Layout = LayoutArquivoImportacao.C6CartaoCsv.Value,
                Arquivo = ArquivoFormulario.ComTamanho(tamanho: 1),
                DiaVencimento = diaUnico,
                Vencimentos = new Dictionary<string, int> { [sufixo] = diaDoFinal },
            });
    }
}
