using Bogus.DataSets;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;

/// <summary>
/// Usuário com nome completo em pt-BR e renda bruta entre R$ 1.500,00 e R$ 30.000,00. Os descontos
/// e o cartão vêm do fixture, então seguem as outras customizações realistas.
/// </summary>
public sealed class UsuarioRealistaCustomization : ICustomization
{
    public const decimal RendaMinima = 1_500m;
    public const decimal RendaMaxima = 30_000m;

    private readonly Random _random;
    private readonly Name _nomes;

    public UsuarioRealistaCustomization(Random random, Name nomes)
    {
        _random = random;
        _nomes = nomes;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Register((IEnumerable<DescontoSalarial> descontos, Cartao cartao) => Usuario.Criar(
            (TextoObrigatorio)_nomes.FullName(),
            (ReaisPositivo)SortearRenda(),
            descontos.ToList(),
            cartao));
    }

    private decimal SortearRenda() =>
        Math.Round(RendaMinima + (decimal)_random.NextDouble() * (RendaMaxima - RendaMinima), 2);
}
