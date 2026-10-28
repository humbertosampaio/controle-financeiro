using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Tests.AutoFixture;
using FluentValidation.TestHelper;

namespace ControleFinanceiro.WebApi.Tests.Beneficiarios.CadastrarLote;

/// <summary>
/// Testes de unidade, sem API nem banco: as regras que o validator acrescenta ao domínio. Nome e alias só delegam aos
/// value objects, já testados no domínio; que cada regra está ligada ao campo certo, o teste de integração do endpoint
/// confere.
/// </summary>
public sealed class CadastrarLoteBeneficiariosValidatorTests
{
    private readonly CadastrarLoteBeneficiariosValidator _sut = new();

    [Theory, ApiAutoData]
    public void Validate_Quando_RequisicaoValida_Deve_Passar(Guid idUsuario, CadastrarLoteBeneficiariosRequest requisicao)
    {
        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(requisicao with { IdUsuario = idUsuario });

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_SemItens_Deve_RetornarErroNosItens(Guid idUsuario)
    {
        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado =
            _sut.TestValidate(new CadastrarLoteBeneficiariosRequest { IdUsuario = idUsuario, Itens = [] });

        resultado.ShouldHaveValidationErrorFor(r => r.Itens);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_ItensAcimaDoLimite_Deve_RetornarErroNosItens(Guid idUsuario, ItemBeneficiarioDto item)
    {
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = Enumerable.Repeat(item, CadastrarLoteBeneficiariosValidator.TamanhoMaximoLote + 1).ToList(),
        };

        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldHaveValidationErrorFor(r => r.Itens);
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_LoteNoLimite_Deve_Passar(Guid idUsuario, ItemBeneficiarioDto item)
    {
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = Enumerable.Repeat(item, CadastrarLoteBeneficiariosValidator.TamanhoMaximoLote).ToList(),
        };

        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_BeneficiarioExistenteSemAlias_Deve_RetornarErroNoAlias(Guid idUsuario, Guid idBeneficiario)
    {
        CadastrarLoteBeneficiariosRequest requisicao = new()
        {
            IdUsuario = idUsuario,
            Itens = [new ItemBeneficiarioDto { IdBeneficiario = idBeneficiario }],
        };

        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(requisicao);

        resultado.ShouldHaveValidationErrorFor("Itens[0].Alias");
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_BeneficiarioExistenteComNome_Deve_RetornarErroNoNome(
        Guid idUsuario,
        Guid idBeneficiario,
        CadastrarLoteBeneficiariosRequest requisicao)
    {
        CadastrarLoteBeneficiariosRequest comNome = requisicao with
        {
            IdUsuario = idUsuario,
            Itens = [requisicao.Itens[0] with { IdBeneficiario = idBeneficiario }],
        };

        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(comNome);

        resultado.ShouldHaveValidationErrorFor("Itens[0].Nome");
    }

    [Theory, ApiAutoData]
    public void Validate_Quando_BeneficiarioNovoSemAlias_Deve_Passar(Guid idUsuario, CadastrarLoteBeneficiariosRequest requisicao)
    {
        CadastrarLoteBeneficiariosRequest semAlias = requisicao with
        {
            IdUsuario = idUsuario,
            Itens = [requisicao.Itens[0] with { Alias = null }],
        };

        TestValidationResult<CadastrarLoteBeneficiariosRequest> resultado = _sut.TestValidate(semAlias);

        resultado.ShouldNotHaveAnyValidationErrors();
    }
}
