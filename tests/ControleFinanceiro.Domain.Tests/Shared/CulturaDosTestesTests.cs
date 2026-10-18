using System.Globalization;

namespace ControleFinanceiro.Domain.Tests.Shared;

public sealed class CulturaDosTestesTests
{
    [Fact]
    public void Testes_Quando_Executados_Deve_UsarCulturaPtBr()
    {
        CultureInfo.CurrentCulture.Name.Should().Be("pt-BR");
        (-87.08m).ToString().Should().Be("-87,08");
    }

    [Fact]
    public async Task Testes_Quando_ExecutadosEmOutraThread_Deve_UsarCulturaPtBr()
    {
        string nome = await Task.Run(() => CultureInfo.CurrentCulture.Name);

        nome.Should().Be("pt-BR");
    }
}
