using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class ImportacaoFaturaTests
{
    [Theory, VencimentoCicloCartaoAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_MesmoVencimento_Deve_SerFalso([Frozen] Cartao cartao, ImportacaoFatura sut)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], sut.Vencimento);

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_OutraDataNoMesmoMes_Deve_SerVerdadeiro(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura sut,
        DiaVencimentoCartao outroDia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeTrue();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_VencimentoEmOutroMes_Deve_SerFalso(
        [Frozen] Cartao cartao,
        ImportacaoFatura sut,
        MesReferencia outroMes,
        DiaVencimentoCartao dia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(outroMes, dia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void TemOutroVencimentoNoMesmoMes_Quando_CartaoForaDaFatura_Deve_SerFalso(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura sut,
        DiaVencimentoCartao outroDia)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        FaturaCartao fatura = FaturaCartao.Importada(cartaoOutroBanco.Banco, [cartaoOutroBanco], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.TemOutroVencimentoNoMesmoMes(fatura).Should().BeFalse();
    }

    // A fatura do cartão é montada com o vencimento projetado, e a importação devolve o real, numa data do mesmo mês.
    [Theory, VencimentoCicloCartaoAutoData]
    public void VencimentoImportado_Quando_ImportacaoDaFatura_Deve_DevolverOVencimentoReal(
        [Frozen] MesReferencia mes,
        [Frozen] Cartao cartao,
        ImportacaoFatura sut,
        DiaVencimentoCartao outroDia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(mes, outroDia));

        sut.VencimentoImportado(fatura).Should().HaveValue(sut.Vencimento);
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void VencimentoImportado_Quando_CartaoForaDaFatura_Deve_SerVazio([Frozen] Cartao cartao, ImportacaoFatura sut)
    {
        Cartao cartaoOutroBanco = cartao.EmOutroBanco();
        FaturaCartao fatura = FaturaCartao.Importada(cartaoOutroBanco.Banco, [cartaoOutroBanco], cartoesUsuario: [], sut.Vencimento);

        sut.VencimentoImportado(fatura).Should().HaveNoValue();
    }

    [Theory, VencimentoCicloCartaoAutoData]
    public void VencimentoImportado_Quando_FaturaDeOutroMes_Deve_SerVazio(
        [Frozen] Cartao cartao,
        ImportacaoFatura sut,
        MesReferencia outroMes,
        DiaVencimentoCartao dia)
    {
        FaturaCartao fatura = FaturaCartao.Importada(cartao.Banco, [cartao], cartoesUsuario: [], VencimentoFatura.Projetado(outroMes, dia));

        sut.VencimentoImportado(fatura).Should().HaveNoValue();
    }
}
