using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// O layout de arquivo que a importação aceita, um por banco e tipo de origem. O código é o que o usuário
/// informa ao importar e o que fica gravado na importação.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Só declara constantes; a busca por código é do SmartEnum.")]
public sealed class LayoutArquivoImportacao : SmartEnum<LayoutArquivoImportacao, string>
{
    public static readonly LayoutArquivoImportacao C6ContaCsv = new("c6-conta-csv", "Extrato CSV da conta do C6");

    private LayoutArquivoImportacao(string codigo, string name) : base(name, codigo)
    {
    }
}
