namespace ControleFinanceiro.Infrastructure.Importacao.Tests.Arquivos;

/// <summary>
/// Um <see cref="FactAttribute"/> para o comportamento que só existe no sistema de arquivos do Windows. Nas outras
/// plataformas, o teste aparece como pulado, e não como aprovado.
/// </summary>
internal sealed class FactSoNoWindowsAttribute : FactAttribute
{
    public FactSoNoWindowsAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "O comportamento é do sistema de arquivos do Windows.";
    }
}
