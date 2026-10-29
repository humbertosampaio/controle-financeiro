using ControleFinanceiro.WebApi.Endpoints.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Categorias.CadastrarLote;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio. Nome e alias só delegam aos
/// value objects, já testados no domínio; que cada regra está ligada ao campo certo, o teste de integração do endpoint
/// confere.
/// </summary>
public sealed class CadastrarLoteCategoriasValidatorTests
{
    private readonly CadastrarLoteCategoriasValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(Guid idUsuario, CadastrarLoteCategoriasRequest requisicao)
    {
        TestValidationResult<CadastrarLoteCategoriasRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemItens_Deve_RetornarErroNosItens(Guid idUsuario)
    {
        TestValidationResult<CadastrarLoteCategoriasRequest> resultado =
            _sut.TestValidate(new CadastrarLoteCategoriasRequest { IdUsuario = idUsuario, Itens = [] });

        resultado.ShouldHaveValidationErrorFor(r => r.Itens);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ItensAcimaDoLimite_Deve_RetornarErroNosItens(Guid idUsuario, ItemCategoriaDto item)
    {
        CadastrarLoteCategoriasRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = Enumerable.Repeat(item, CadastrarLoteCategoriasValidator.TamanhoMaximoLote + 1).ToArray(),
        };

        TestValidationResult<CadastrarLoteCategoriasRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldHaveValidationErrorFor(r => r.Itens);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_LoteNoLimite_Deve_Passar(Guid idUsuario, ItemCategoriaDto item)
    {
        CadastrarLoteCategoriasRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = Enumerable.Repeat(item, CadastrarLoteCategoriasValidator.TamanhoMaximoLote).ToArray(),
        };

        TestValidationResult<CadastrarLoteCategoriasRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_CategoriaExistenteSemAlias_Deve_RetornarErroNoAlias(Guid idUsuario, Guid idCategoria)
    {
        CadastrarLoteCategoriasRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = [new ItemCategoriaDto { IdCategoria = idCategoria }],
        };

        TestValidationResult<CadastrarLoteCategoriasRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldHaveValidationErrorFor("Itens[0].Alias");
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_CategoriaNovaSemAlias_Deve_Passar(Guid idUsuario, CadastrarLoteCategoriasRequest requisicao)
    {
        CadastrarLoteCategoriasRequest semAlias = requisicao with
        {
            IdUsuario = idUsuario,
            Itens = [requisicao.Itens[0] with { Alias = null }],
        };

        TestValidationResult<CadastrarLoteCategoriasRequest> resultado = _sut.TestValidate(semAlias);

        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
