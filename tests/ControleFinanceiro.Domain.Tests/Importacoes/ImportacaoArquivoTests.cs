using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ImportacaoArquivoTests
{
    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_PrimeiraImportacao_Deve_RetornarNada(ImportacaoArquivo sut)
    {
        Maybe<Periodo> result = sut.PeriodoNaoImportado(Maybe.None);

        result.Should().HaveNoValue();
    }

    [Theory]
    [InlineDomainAutoData(1)]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(-5)]
    public void PeriodoNaoImportado_Quando_PeriodoContiguoOuSobreposto_Deve_RetornarNada(
        int diasDoFimAnteriorAoInicio,
        ContaBancaria conta,
        Periodo periodoAnterior,
        Fixture fixture)
    {
        ImportacaoArquivo ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(diasDoFimAnteriorAoInicio);
        ImportacaoArquivo sut = Com(fixture, conta, new Periodo(inicio, inicio.AddDays(30)));

        Maybe<Periodo> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveNoValue();
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_DiasEntreAsImportacoes_Deve_RetornarOsDiasFaltantes(
        ContaBancaria conta,
        Periodo periodoAnterior,
        Fixture fixture)
    {
        ImportacaoArquivo ultima = Com(fixture, conta, periodoAnterior);
        DateOnly inicio = ultima.Periodo.Fim.AddDays(4);
        ImportacaoArquivo sut = Com(fixture, conta, new Periodo(inicio, inicio.AddDays(30)));

        Maybe<Periodo> result = sut.PeriodoNaoImportado(ultima);

        result.Should().HaveValue(new Periodo(ultima.Periodo.Fim.AddDays(1), ultima.Periodo.Fim.AddDays(3)));
    }

    [Theory, DomainAutoData]
    public void PeriodoNaoImportado_Quando_UltimaDeOutraOrigem_Deve_LancarExcecao(
        ImportacaoArquivo sut,
        ImportacaoArquivo deOutraOrigem)
    {
        Action conferir = () => sut.PeriodoNaoImportado(deOutraOrigem);

        conferir.Should().ThrowExactly<ArgumentException>().WithParameterName("ultima");
    }

    [Theory, DomainAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_Extrato_Deve_SerFalso(ImportacaoArquivo sut, Cartao cartao, VencimentoFatura vencimento)
    {
        sut.TemOutroVencimentoNoMesmoMes(FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], vencimento)).Should().BeFalse();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_MesmoVencimento_Deve_SerFalso([Frozen] Cartao cartao, ImportacaoArquivo sut)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], sut.VencimentoFatura!);

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_OutraDataNoMesmoMes_Deve_SerVerdadeiro(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoArquivo sut,
        DiaVencimentoCartao outroDia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeTrue();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_VencimentoEmOutroMes_Deve_SerFalso(
        [Frozen] Cartao cartao,
        ImportacaoArquivo sut,
        MesReferencia outroMes,
        DiaVencimentoCartao dia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(outroMes, dia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_CartaoForaDaFatura_Deve_SerFalso(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoArquivo sut,
        DiaVencimentoCartao outroDia)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        FaturaCartao fatura = FaturaCartao.Importada(cartaoOutroBanco.Banco, [cartaoOutroBanco], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    // A fatura do cartão é montada com o vencimento projetado, e a importação devolve o real, numa data do mesmo mês.
    [Theory, ImportacaoFaturaAutoData]
    public void VencimentoImportado_Quando_ImportacaoDaFatura_Deve_DevolverOVencimentoReal(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoArquivo sut,
        DiaVencimentoCartao outroDia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.VencimentoImportado(fatura).Should().HaveValue(sut.VencimentoFatura!);
    }

    [Theory, DomainAutoData]
    public void VencimentoImportado_Quando_Extrato_Deve_SerVazio(ImportacaoArquivo sut, Cartao cartao, VencimentoFatura vencimento)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], vencimento);

        sut.VencimentoImportado(fatura).Should().HaveNoValue();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void VencimentoImportado_Quando_CartaoForaDaFatura_Deve_SerVazio([Frozen] Cartao cartao, ImportacaoArquivo sut)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        FaturaCartao fatura = FaturaCartao.Importada(cartaoOutroBanco.Banco, [cartaoOutroBanco], cartoesUsuario: [], sut.VencimentoFatura!);

        sut.VencimentoImportado(fatura).Should().HaveNoValue();
    }

    [Theory, ImportacaoFaturaAutoData]
    public void VencimentoImportado_Quando_FaturaDeOutroMes_Deve_SerVazio(
        [Frozen] Cartao cartao,
        ImportacaoArquivo sut,
        MesReferencia outroMes,
        DiaVencimentoCartao dia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(outroMes, dia));

        sut.VencimentoImportado(fatura).Should().HaveNoValue();
    }

    private static ImportacaoArquivo Com(Fixture fixture, ContaBancaria conta, Periodo periodo)
    {
        fixture.Inject(conta);
        fixture.Inject(periodo);
        return fixture.Create<ImportacaoArquivo>();
    }
}
