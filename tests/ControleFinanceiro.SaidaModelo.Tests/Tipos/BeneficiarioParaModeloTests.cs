using System.Text.Json.Nodes;
using ControleFinanceiro.Application.SaidaModelo;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class BeneficiarioParaModeloTests
{
    [Theory, DomainAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(Beneficiario beneficiario)
    {
        JsonObject json = beneficiario.ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "nome", "aliases");
    }

    [Theory, DomainAutoData]
    public void Serializar_Deve_LevarOIdDoBeneficiario(Beneficiario beneficiario)
    {
        JsonObject json = beneficiario.ParaModelo().Serializar();

        json["id"]!.GetValue<Guid>().Should().Be(beneficiario.Id.Value);
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_NomeSoComDocumento_Deve_SairVazio(DocumentosDeTeste documentos, Beneficiario beneficiario)
    {
        beneficiario.Renomear((TextoObrigatorio)documentos.Em(FormaDocumento.CpfPuro));

        JsonObject json = beneficiario.ParaModelo().Serializar();

        json.Texto("nome").Should().BeEmpty();
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    public void Serializar_Quando_NomeBeneficiarioComDocumento_Deve_SairSemODocumento(FormaDocumento forma, DocumentosDeTeste documentos, Beneficiario beneficiario)
    {
        beneficiario.Renomear((TextoObrigatorio)$"FULANO {documentos.Em(forma)}");

        JsonObject json = beneficiario.ParaModelo().Serializar();

        json.Texto("nome").Should().Be($"FULANO {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetras)]
    public void Serializar_Quando_AliasComDocumento_Deve_SairSemODocumento(FormaDocumento forma, DocumentosDeTeste documentos, Fixture fixture)
    {
        fixture.Inject((Alias)$"FULANO {documentos.Em(forma)}");

        JsonObject json = fixture.Create<Beneficiario>().ParaModelo().Serializar();

        Aliases(json).Should().Equal($"FULANO {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_AliasSoComDocumento_Deve_TirarDaLista(DocumentosDeTeste documentos, Fixture fixture)
    {
        fixture.Inject((Alias)documentos.Em(FormaDocumento.CpfPuro));

        JsonObject json = fixture.Create<Beneficiario>().ParaModelo().Serializar();

        Aliases(json).Should().BeEmpty();
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_AliasesIguaisDepoisDaMascara_Deve_SairUmSo(Cpf cpf, Cpf outroCpf, Fixture fixture)
    {
        fixture.Inject((Alias)$"FULANO {cpf}");
        Beneficiario beneficiario = fixture.Create<Beneficiario>();
        Alias outro = (Alias)$"FULANO {outroCpf}";
        BeneficiarioFactory.Resolver(beneficiario.IdUsuario, [beneficiario], [new AliasBeneficiarioExistente(beneficiario, outro)]).Should().Succeed();

        JsonObject json = beneficiario.ParaModelo().Serializar();

        Aliases(json).Should().Equal("FULANO [CPF]");
    }

    private static IEnumerable<string> Aliases(JsonObject json) => json["aliases"]!.AsArray().Select(alias => alias!.GetValue<string>());
}
