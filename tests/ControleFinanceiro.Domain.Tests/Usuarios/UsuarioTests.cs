using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Usuarios;

public sealed class UsuarioTests
{
    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarOsDescontosSalariais(TextoObrigatorio nome, ReaisPositivo rendaBruta, DescontoSalarial primeiro, DescontoSalarial segundo)
    {
        Usuario sut = Usuario.Criar(nome, rendaBruta, [primeiro, segundo]);

        sut.DescontosSalariais.Should().Equal(primeiro, segundo);
    }

    [Theory, DomainAutoData]
    public void RendaLiquida_Quando_SemDescontos_Deve_RetornarRendaBruta(TextoObrigatorio nome, ReaisPositivo rendaBruta)
    {
        Usuario sut = Usuario.Criar(nome, rendaBruta, descontosSalariais: []);

        decimal result = sut.RendaLiquida;

        result.Should().Be(rendaBruta);
    }

    [Fact]
    public void RendaLiquida_Quando_ComDescontos_Deve_RetornarRendaBrutaMenosSomaDosValores()
    {
        ReaisPositivo rendaBruta = (ReaisPositivo)1000m;
        DescontoSalarial desconto1 = DescontoSalarial.Criar((TextoObrigatorio)"INSS", (ReaisPositivo)100m);
        DescontoSalarial desconto2 = DescontoSalarial.Criar((TextoObrigatorio)"IR", (ReaisPositivo)50m);
        Usuario sut = Usuario.Criar((TextoObrigatorio)"Nome", rendaBruta, [desconto1, desconto2]);

        decimal result = sut.RendaLiquida;

        result.Should().Be(850m);
    }
}
