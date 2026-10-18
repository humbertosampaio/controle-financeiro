using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class SufixoCartaoConverter : ValueConverter<SufixoCartao, string>
{
    public SufixoCartaoConverter() : base(
        sufixo => sufixo.Value,
        valor => (SufixoCartao)valor)
    {
    }
}
