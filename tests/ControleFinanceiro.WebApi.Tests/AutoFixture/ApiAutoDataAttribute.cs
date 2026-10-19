using AutoFixture.Xunit2;

namespace ControleFinanceiro.WebApi.Tests.AutoFixture;

public sealed class ApiAutoDataAttribute : AutoDataAttribute
{
    public ApiAutoDataAttribute() : base(ApiFixture.Create)
    {
    }
}
