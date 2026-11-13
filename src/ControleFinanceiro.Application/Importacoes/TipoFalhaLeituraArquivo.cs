using Ardalis.SmartEnum;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O motivo fixo por que o leitor recusou o arquivo. É o que a saída para o modelo usa, no lugar da mensagem, que pode
/// trazer datas do arquivo.
/// </summary>
public sealed class TipoFalhaLeituraArquivo : SmartEnum<TipoFalhaLeituraArquivo>
{
    /// <summary>Uma linha que não está no layout: o CSV não a entende, ou ela não tem a forma que o layout pede.</summary>
    public static readonly TipoFalhaLeituraArquivo ForaDoLayout = new(nameof(ForaDoLayout), 0);

    /// <summary>O arquivo não traz as colunas do layout, ou termina antes delas.</summary>
    public static readonly TipoFalhaLeituraArquivo SemColunas = new(nameof(SemColunas), 1);

    /// <summary>Uma linha no layout, mas com um lançamento que a importação não aceita (sem descrição, por exemplo).</summary>
    public static readonly TipoFalhaLeituraArquivo LancamentoInvalido = new(nameof(LancamentoInvalido), 2);

    /// <summary>
    /// O arquivo inteiro, sem uma linha só a apontar: o cabeçalho sem a conta ou o período, os saldos que não fecham,
    /// o arquivo sem lançamentos.
    /// </summary>
    public static readonly TipoFalhaLeituraArquivo ConteudoInvalido = new(nameof(ConteudoInvalido), 3);

    private TipoFalhaLeituraArquivo(string name, int value) : base(name, value)
    {
    }
}
