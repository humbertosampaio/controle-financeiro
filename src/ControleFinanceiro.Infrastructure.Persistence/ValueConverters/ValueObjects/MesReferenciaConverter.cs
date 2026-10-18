using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class MesReferenciaConverter : ValueConverter<MesReferencia, DateOnly>
{
    public MesReferenciaConverter() : base(
        mes => mes.Inicio,
        data => MesReferencia.Criar(data.Month, data.Year).Value)
    {
    }
}
