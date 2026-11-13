namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Por que o leitor recusou o arquivo. A <see cref="Mensagem"/> é o texto que a API devolve ao usuário e pode citar as
/// datas do arquivo; a saída para o modelo usa só a <see cref="Linha"/> e o <see cref="Tipo"/>.
/// </summary>
/// <param name="Linha">O número da linha do arquivo, contado a partir de 1; vazio quando a falha é do arquivo inteiro.</param>
/// <param name="Tipo">O motivo fixo.</param>
/// <param name="Mensagem">Nunca traz o conteúdo de uma linha.</param>
public sealed record FalhaLeituraArquivo(long? Linha, TipoFalhaLeituraArquivo Tipo, string Mensagem)
{
    /// <summary>A falha de uma linha, com a mensagem "A linha N do arquivo ...".</summary>
    /// <param name="fimDaFrase">O que vem depois de "A linha N do arquivo", com o ponto final.</param>
    public static FalhaLeituraArquivo NaLinha(TipoFalhaLeituraArquivo tipo, long linha, string fimDaFrase)
    {
        return new FalhaLeituraArquivo(linha, tipo, $"A linha {linha} do arquivo {fimDaFrase}");
    }

    /// <summary>A falha do arquivo inteiro, sem uma linha a apontar.</summary>
    public static FalhaLeituraArquivo NoArquivo(TipoFalhaLeituraArquivo tipo, string mensagem)
    {
        return new FalhaLeituraArquivo(Linha: null, tipo, mensagem);
    }
}
