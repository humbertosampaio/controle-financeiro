using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Categorias;

public sealed class CategoriaDespesaTests
{
    [Theory, DomainAutoData]
    public void Criar_Quando_ParametrosValidos_Deve_RetornarSucesso(IdUsuario idUsuario, TextoObrigatorio nome)
    {
        CategoriaDespesa categoria = CategoriaDespesa.Criar(idUsuario, nome);

        categoria.IdUsuario.Should().Be(idUsuario);
        categoria.Nome.Should().Be(nome);
    }

    [Theory, DomainAutoData]
    public void Renomear_Quando_NovoNomeValido_Deve_AlterarNome(CategoriaDespesa categoria, TextoObrigatorio novoNome)
    {
        categoria.Renomear(novoNome);

        categoria.Nome.Should().Be(novoNome);
    }
}
