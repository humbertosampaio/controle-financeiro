using ControleFinanceiro.Application.IA;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Application.Importacoes;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Prevê a importação sem gravar. O sufixo da conta e o vencimento são opcionais com o texto vazio, e não com o nulo: o <c>string?</c>
/// sai no esquema com o tipo em lista (<c>["string", "null"]</c>), que o cliente descarta, e sem o tipo o modelo manda o
/// sufixo como número, que o SDK não converte para texto. Vazios, o sufixo e o vencimento valem como não informados.
/// </summary>
[McpServerToolType]
internal sealed class PreverImportacaoTool
{
    [McpServerTool(Name = Tools.PreverImportacao, ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description($"Mostra, sem gravar, como fica a importação de um arquivo de extrato ou de fatura da pasta dados/ do repositório, pelo caminho e pelo layout do arquivo. Layouts: c6-conta-csv (extrato da conta do C6), mercado-pago-conta-csv (extrato da conta do Mercado Pago, que pede o sufixo da conta), c6-cartao-csv (fatura do cartão do C6, que pede o vencimento da fatura) e mercado-pago-cartao-csv (fatura do cartão do Mercado Pago). Devolve o banco, o período, as contagens de lançamentos lidos, novos e já existentes, os cartões da fatura, se a conta será criada e o resumo, ou o erro, com o que o usuário precisa cadastrar pela API. Nada é gravado: mostre o resumo ao usuário e, se ele quiser a importação, chame {Tools.ConfirmarImportacao} com os mesmos argumentos e o resumo sem alteração.")]
    public static Task<ImportacaoParaModelo> Prever(
        IOrigemArquivoImportacao origem,
        ImportacaoParaModeloService importacao,
        [Description("O caminho do arquivo, absoluto ou relativo à pasta que contém a pasta de origem (a raiz do repositório, por padrão), como dados/2026/extrato.csv.")] string caminho,
        [Description("O código do layout do arquivo.")] string layout,
        [Description("Só no layout mercado-pago-conta-csv, em que é obrigatório: os 4 últimos caracteres do número da conta, que o usuário informa.")] string sufixoConta = "",
        [Description("Só no layout c6-cartao-csv, em que é obrigatório: a data de vencimento da fatura em AAAA-MM-DD, que o usuário informa.")] string vencimento = "",
        CancellationToken cancellationToken = default)
    {
        return ArquivoDaOrigem.ImportarAsync(
            origem,
            caminho,
            arquivo => importacao.PreverAsync(arquivo, layout, sufixoConta, vencimento, cancellationToken),
            cancellationToken);
    }
}
