using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;

/// <summary>
/// Desconto com descrição de contracheque. O valor vem do fixture: os poucos reais com centavos que o
/// AutoFixture gera ficam, somados, bem abaixo da menor renda da <see cref="UsuarioRealistaCustomization"/>.
/// </summary>
public sealed class DescontoSalarialRealistaCustomization : ICustomization
{
    private static readonly string[] _descricoes =
    [
        "INSS",
        "IRRF",
        "Plano de saúde",
        "Plano odontológico",
        "Vale-transporte",
        "Vale-refeição",
        "Previdência privada",
        "Contribuição sindical",
        "Seguro de vida",
    ];

    private readonly Random _random;

    public DescontoSalarialRealistaCustomization(Random random)
    {
        _random = random;
    }

    public void Customize(IFixture fixture)
    {
        fixture.Register((ReaisPositivo valor) => DescontoSalarial.Criar(
            (TextoObrigatorio)_descricoes[_random.Next(_descricoes.Length)],
            valor));
    }
}
