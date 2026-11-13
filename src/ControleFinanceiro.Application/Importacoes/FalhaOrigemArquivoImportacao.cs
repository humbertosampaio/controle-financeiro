using Ardalis.SmartEnum;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>Por que a <see cref="IOrigemArquivoImportacao"/> não abriu o arquivo pedido.</summary>
public sealed class FalhaOrigemArquivoImportacao : SmartEnum<FalhaOrigemArquivoImportacao, string>
{
    public static readonly FalhaOrigemArquivoImportacao OrigemIndisponivel =
        new("origem-indisponivel", "A pasta de origem da importação não existe.");

    public static readonly FalhaOrigemArquivoImportacao ForaDaOrigem =
        new("fora-da-origem", "O caminho não aponta para um arquivo dentro da pasta de origem da importação.");

    public static readonly FalhaOrigemArquivoImportacao ExtensaoNaoAceita =
        new("extensao-nao-aceita", "A importação só lê arquivos .csv.");

    public static readonly FalhaOrigemArquivoImportacao NaoEncontrado =
        new("nao-encontrado", "O arquivo não existe na pasta de origem da importação.");

    public static readonly FalhaOrigemArquivoImportacao GrandeDemais =
        new("grande-demais", "O arquivo passa do tamanho máximo da importação.");

    private FalhaOrigemArquivoImportacao(string codigo, string name) : base(name, codigo)
    {
    }
}
