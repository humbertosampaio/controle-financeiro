using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar;

/// <summary>
/// Monta o <see cref="CadastrarUsuarioRequest"/> a partir de um <see cref="Usuario"/> do fixture: como os
/// value objects só existem válidos, a requisição também é. Com <c>[Frozen] Usuario</c> no teste, a
/// requisição sai do mesmo usuário que o teste espera encontrar no banco.
/// </summary>
internal sealed class CadastrarUsuarioRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Usuario usuario) => new CadastrarUsuarioRequest
        {
            Nome = usuario.Nome.Value,
            RendaBruta = usuario.RendaBruta.Value,
            DescontosSalariais = usuario.DescontosSalariais
                .Select(desconto => new DescontoSalarialDto
                {
                    Descricao = desconto.Descricao.Value,
                    Valor = desconto.Valor.Value,
                })
                .ToList(),
        });
    }
}
