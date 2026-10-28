using ControleFinanceiro.Tests.Shared.AutoFixture;
using ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Realistas;
using ControleFinanceiro.WebApi.Tests.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Tests.Beneficiarios.RemoverAlias;
using ControleFinanceiro.WebApi.Tests.Beneficiarios.Renomear;
using ControleFinanceiro.WebApi.Tests.Categorias.CadastrarLote;
using ControleFinanceiro.WebApi.Tests.Categorias.RemoverAlias;
using ControleFinanceiro.WebApi.Tests.Categorias.Renomear;
using ControleFinanceiro.WebApi.Tests.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Importacoes.PreVisualizarFatura;
using ControleFinanceiro.WebApi.Tests.Usuarios.AdicionarCartao;
using ControleFinanceiro.WebApi.Tests.Usuarios.AlterarVencimentoCartao;
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
            .Customize(new CadastrarUsuarioRequestCustomization())
            .Customize(new AdicionarCartaoRequestCustomization())
            .Customize(new AlterarVencimentoCartaoRequestCustomization())
            .Customize(new ImportarExtratoRequestCustomization())
            .Customize(new ImportarFaturaRequestCustomization())
            .Customize(new PreVisualizarFaturaRequestCustomization())
            .Customize(new CadastrarLoteCategoriasRequestCustomization())
            .Customize(new RenomearCategoriaRequestCustomization())
            .Customize(new RemoverAliasCategoriaRequestCustomization())
            .Customize(new CadastrarLoteBeneficiariosRequestCustomization())
            .Customize(new RenomearBeneficiarioRequestCustomization())
            .Customize(new RemoverAliasBeneficiarioRequestCustomization())
            .Customize(new CadastrarDespesaRecorrenteRequestCustomization());
    }

    public static new ApiFixture Create() => new();
}
