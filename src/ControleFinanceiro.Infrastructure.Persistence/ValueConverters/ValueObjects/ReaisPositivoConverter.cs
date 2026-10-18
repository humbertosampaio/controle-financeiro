using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class ReaisPositivoConverter : ValueConverter<ReaisPositivo, decimal>
{
    public ReaisPositivoConverter() : base(
        reais => reais.Value,
        valor => (ReaisPositivo)valor)
    {
    }
}
