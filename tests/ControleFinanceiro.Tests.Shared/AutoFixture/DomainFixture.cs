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
            .Customize(new CpfCustomization())
            .Customize(new CnpjCustomization())
            .Customize(new MesReferenciaCustomization())
            .Customize(new DiaVencimentoCartaoCustomization())
            .Customize(new SufixoCartaoCustomization())
            .Customize(new VencimentosCartoesNovosCustomization())
            .Customize(new SufixoContaCustomization())
            .Customize(new CartaoCustomization())
            .Customize(new ContaBancariaCustomization())
            .Customize(new DespesaRecorrenteCustomization())
            .Customize(new ParcelaCustomization())
            .Customize(new LancamentoCustomization())
            .Customize(new IdentidadeLancamentoCustomization())
            .Customize(new OrigemTransacaoCustomization())
            .Customize(new PeriodoArquivoCustomization())
            .Customize(new ExtratoCustomization())
            .Customize(new FaturaCustomization())
            .Customize(new ImportacaoArquivoCustomization())
            .Customize(new DiaOcorrenciaRecorrenteCustomization())
            .Customize(new TextoObrigatorioCustomization());
    }

    public static DomainFixture Create() => new();
}
