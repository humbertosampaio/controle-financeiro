using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class ReaisConverter : ValueConverter<Reais, decimal>
{
    public ReaisConverter() : base(
        reais => reais.Value,
        valor => new(valor))
    {
    }
}
