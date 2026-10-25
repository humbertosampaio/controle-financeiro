using Microsoft.AspNetCore.Http;

namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

/// <summary>
/// O campo <c>arquivo</c> de um formulário de importação para os testes do validator, que só lê o tamanho declarado:
/// o conteúdo é do leitor, e o teste do endpoint manda o arquivo fictício de verdade.
/// </summary>
internal static class ArquivoFormulario
{
    /// <summary>Um arquivo vazio que declara ter <paramref name="tamanho"/> bytes.</summary>
    public static FormFile ComTamanho(long tamanho) =>
        new(Stream.Null, baseStreamOffset: 0, tamanho, name: "arquivo", fileName: "arquivo.csv");
}
