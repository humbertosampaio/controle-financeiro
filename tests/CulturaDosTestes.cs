using System.Globalization;
using System.Runtime.CompilerServices;
using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Tests;

/// <summary>
/// Faz todos os testes rodarem em pt-BR, a cultura do app, qualquer que seja a da máquina
/// (o runner do CI usa en-US). Entra em todo projeto sob <c>tests/</c> pelo
/// <c>tests/Directory.Build.props</c>, então nenhum teste precisa fixar a cultura.
/// </summary>
internal static class CulturaDosTestes
{
    // CA2255 desaconselha ModuleInitializer em bibliotecas; aqui o assembly é de testes e o
    // inicializador existe justamente para rodar antes de qualquer teste.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Aplicar()
    {
        CultureInfo.DefaultThreadCurrentCulture = AppDefaults.CulturaPtBr;
        CultureInfo.DefaultThreadCurrentUICulture = AppDefaults.CulturaPtBr;
        CultureInfo.CurrentCulture = AppDefaults.CulturaPtBr;
        CultureInfo.CurrentUICulture = AppDefaults.CulturaPtBr;
    }
}
