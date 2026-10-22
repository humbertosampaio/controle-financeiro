using ControleFinanceiro.Domain.Cartoes;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class DiaVencimentoCartaoConverter : ValueConverter<DiaVencimentoCartao, int>
{
    public DiaVencimentoCartaoConverter() : base(
        dia => dia.Value,
        valor => (DiaVencimentoCartao)valor)
    {
    }
}
