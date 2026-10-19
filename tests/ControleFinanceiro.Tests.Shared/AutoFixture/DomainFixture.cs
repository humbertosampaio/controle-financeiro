using Ardalis.SmartEnum.AutoFixture;
using AutoFixture.AutoNSubstitute;
using ControleFinanceiro.Tests.Shared.AutoFixture.Customizations;
using ControleFinanceiro.Tests.Shared.AutoFixture.Customizations.Domain.Shared;

namespace ControleFinanceiro.Tests.Shared.AutoFixture;

public class DomainFixture : Fixture
{
    public DomainFixture() : base()
    {
        this.Customize(new AutoNSubstituteCustomization() { ConfigureMembers = true })
            .Customize(new SmartEnumCustomization())
            .Customize(new DateOnlyCustomization())
            .Customize(new TimeProviderCustomization())
            .Customize(new AsyncEnumerableCustomization())
            .Customize(new StrictDecimalCustomization())
            .Customize(new VogenCustomization())
            .Customize(new ReaisPositivoCustomization())
            .Customize(new MesReferenciaCustomization())
            .Customize(new DiaVencimentoCartaoCustomization())
            .Customize(new SufixoCartaoCustomization())
            .Customize(new DespesaRecorrenteCustomization())
            .Customize(new DiaCompraRecorrenteCustomization())
            .Customize(new TextoObrigatorioCustomization())
            .Customize(new EntityIdCustomization());
    }

    public static DomainFixture Create() => new();
}
