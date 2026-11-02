using System.Text.Json.Nodes;
using ControleFinanceiro.Application.SaidaModelo;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class DespesaParaModeloTests
{
    [Theory, DomainAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(DespesaSimples despesa)
    {
        JsonObject json = despesa.ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "descricao", "dataOcorrencia");
    }

    [Theory, DomainAutoData]
    public void Serializar_Deve_LevarOsValoresDaDespesa(DespesaSimples despesa)
    {
        JsonObject json = despesa.ParaModelo().Serializar();

        json["id"]!.GetValue<Guid>().Should().Be(despesa.Id.Value);
        DateOnly.ParseExact(json.Texto("dataOcorrencia"), "yyyy-MM-dd").Should().Be(despesa.DataOcorrencia);
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Serializar_Quando_DescricaoDespesaParceladaComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)$"LOJA {documentos.Em(forma)}");

        JsonObject json = fixture.Create<DespesaParcelada>().ParaModelo().Serializar();

        json.Texto("descricao").Should().Be($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_DescricaoSoComDocumento_Deve_SairVazia(DocumentosDeTeste documentos, Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)documentos.Em(FormaDocumento.CpfPuro));

        JsonObject json = fixture.Create<DespesaParcelada>().ParaModelo().Serializar();

        json.Texto("descricao").Should().BeEmpty();
    }
}
