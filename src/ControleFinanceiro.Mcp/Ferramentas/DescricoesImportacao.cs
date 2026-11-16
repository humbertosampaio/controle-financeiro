namespace ControleFinanceiro.Mcp.Ferramentas;

/// <summary>
/// Os trechos comuns às descrições das ferramentas de importação, uma prévia e uma confirmação por layout. Cada
/// <c>[Description]</c> monta a sua por interpolação constante; o arquivo de cada layout fica literal nela, porque o
/// nome do layout não é constante.
/// </summary>
internal static class DescricoesImportacao
{
    public const string AberturaPrevia = "Mostra, sem gravar, como fica a importação de um arquivo da pasta dados/ do repositório:";

    public const string RetornoPreviaExtrato = "Devolve o banco, o período, as contagens de lançamentos lidos, novos e já existentes, se a conta será criada e o resumo, ou o erro, com o que o usuário precisa cadastrar pela API.";

    public const string RetornoPreviaFatura = "Devolve o banco, o período, o vencimento, as contagens de lançamentos lidos, novos e já existentes, os cartões da fatura e o resumo, ou o erro, com o que o usuário precisa cadastrar pela API.";

    /// <summary>O começo da frase que termina com o nome da confirmação e o <see cref="FimPrevia"/>.</summary>
    public const string FechoPrevia = "Nada é gravado: mostre o resumo ao usuário e, se ele quiser a importação, chame";

    public const string FimPrevia = "com os mesmos argumentos e o resumo sem alteração.";

    public const string FechoConfirmacao = "O usuário aprova a chamada no cliente, lendo o resumo; sem a aprovação, nada é gravado. Use os mesmos argumentos da prévia e o resumo dela sem alteração: o arquivo é lido de novo, e se a importação recalculada não tiver esse resumo, por exemplo porque o arquivo mudou ou já foi importado, nada é gravado. Devolve o mesmo que a prévia, ou o erro.";

    public const string Caminho = "O caminho do arquivo, absoluto ou relativo à pasta que contém a pasta de origem (a raiz do repositório, por padrão), como dados/2026/extrato.csv.";

    public const string CaminhoConfirmacao = "O caminho do arquivo, o mesmo da prévia.";

    public const string SufixoConta = "Os 4 últimos caracteres do número da conta, que o arquivo não traz e o usuário informa.";

    public const string SufixoContaConfirmacao = "O sufixo da conta, o mesmo da prévia.";

    public const string Vencimento = "A data de vencimento da fatura em AAAA-MM-DD, que o arquivo não traz e o usuário informa.";

    public const string VencimentoConfirmacao = "O vencimento da fatura em AAAA-MM-DD, o mesmo da prévia.";
}
