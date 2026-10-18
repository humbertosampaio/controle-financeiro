using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;

internal sealed class TextoObrigatorioConverter : ValueConverter<TextoObrigatorio, string>
{
    public TextoObrigatorioConverter() : base(
        textoObrigatorio => textoObrigatorio.Value,
        texto => (TextoObrigatorio)texto)
    {

    }
}
