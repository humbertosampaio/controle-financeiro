using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class DiaCompraRecorrenteConverter : ValueConverter<DiaCompraRecorrente, int>
{
    public DiaCompraRecorrenteConverter() : base(
        dia => dia.Value,
        valor => (DiaCompraRecorrente)valor)
    {
    }
}
