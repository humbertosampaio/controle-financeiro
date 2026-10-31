using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class AlteracaoCategoriaDespesaServiceTests
{
    [Theory, DomainAutoData]
    public void Alterar_Quando_CategoriaDoMesmoUsuario_Deve_TrocarACategoria(
        [Frozen] IdUsuario idUsuario,
        DespesaParcelada despesa,
        CategoriaDespesa categoria)
    {
        despesa.IdUsuario.Should().Be(idUsuario);

        AlteracaoCategoriaDespesaService.Alterar(despesa, categoria);

        despesa.IdCategoria.Should().Be(categoria.Id);
    }

    [Theory, DomainAutoData]
    public void Alterar_Quando_CategoriaDeOutroUsuario_Deve_LancarExcecao(DespesaParcelada despesa, CategoriaDespesa categoria)
    {
        Action act = () => AlteracaoCategoriaDespesaService.Alterar(despesa, categoria);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("categoria");
    }

    [Theory, DomainAutoData]
    public void Alterar_Quando_CategoriaDeOutroUsuario_Deve_ManterACategoriaAtual(DespesaParcelada despesa, CategoriaDespesa categoria)
    {
        IdCategoriaDespesa? categoriaOriginal = despesa.IdCategoria;

        Action act = () => AlteracaoCategoriaDespesaService.Alterar(despesa, categoria);

        act.Should().Throw<ArgumentException>();
        despesa.IdCategoria.Should().Be(categoriaOriginal);
    }
}
