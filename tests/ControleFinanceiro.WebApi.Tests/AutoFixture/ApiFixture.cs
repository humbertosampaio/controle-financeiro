using ControleFinanceiro.Tests.Shared.AutoFixture;
using ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;
using ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar;

namespace ControleFinanceiro.WebApi.Tests.AutoFixture;

/// <summary>
/// O <see cref="DomainFixture"/> com o que os testes de integração acrescentam: valores realistas e
/// requisições válidas montadas a partir das entidades.
/// </summary>
public sealed class ApiFixture : DomainFixture
{
    public ApiFixture()
    {
        this.Customize(new RealistasCustomization())
            .Customize(new CadastrarUsuarioRequestCustomization());
    }

    public static new ApiFixture Create() => new();
}
