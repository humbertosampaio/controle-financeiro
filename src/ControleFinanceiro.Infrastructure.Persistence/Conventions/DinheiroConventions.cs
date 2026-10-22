using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.Conventions;

/// <summary>
/// Toda coluna de dinheiro vira <c>numeric(14,2)</c>: as propriedades <see cref="Reais"/>,
/// <see cref="ReaisPositivo"/> e <see cref="decimal"/>, sem configuração por propriedade.
/// </summary>
internal static class DinheiroConventions
{
    /// <summary>
    /// Quantos dígitos, com os centavos, uma coluna de dinheiro guarda: até R$ 999.999.999.999,99. É limite de
    /// armazenamento, e não regra do domínio, que só fixa as casas decimais.
    /// </summary>
    internal const int Precisao = 14;

    extension(ModelConfigurationBuilder configurationBuilder)
    {
        public void ConfigurarDinheiro()
        {
            configurationBuilder.Dinheiro<Reais, ReaisConverter>();
            configurationBuilder.Dinheiro<ReaisPositivo, ReaisPositivoConverter>();

            configurationBuilder
                .Properties<decimal>()
                .HavePrecision(Precisao, Reais.CasasDecimais);
        }

        // A conversão fica na mesma convenção: sem ela o EF não reconhece o value object como coluna.
        private void Dinheiro<TReais, TConverter>()
            where TReais : Reais
            where TConverter : ValueConverter<TReais, decimal>
        {
            configurationBuilder
                .Properties<TReais>()
                .HaveConversion<TConverter>()
                .HavePrecision(Precisao, Reais.CasasDecimais);
        }
    }
}
