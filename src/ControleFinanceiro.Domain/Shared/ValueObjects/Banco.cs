using System.Diagnostics.CodeAnalysis;
using Ardalis.SmartEnum;

namespace ControleFinanceiro.Domain.Shared.ValueObjects;

/// <summary>
/// Banco emissor, identificado pelo código COMPE: os 3 dígitos da compensação bancária. O código é um
/// identificador, não um número, e guarda os zeros à esquerda (o Banco do Brasil é <c>001</c>).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Só declara constantes; a busca por código é do SmartEnum.")]
public sealed class Banco : SmartEnum<Banco, string>
{
    public const int QuantidadeDigitosCodigoCompe = 3;

    public static readonly Banco MercadoPago = new("323", "Mercado Pago");
    public static readonly Banco C6 = new("336", "C6 Bank");

    private Banco(string codigoCompe, string name) : base(name, codigoCompe)
    {
    }
}
