using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class ImportacaoArquivoFactoryTests
{
    [Theory, DomainAutoData]
    public void Registrar_Quando_Conta_Deve_GuardarOsDadosDaImportacaoComOUsuarioEAOrigemDaConta(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoArquivo result = ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.IdUsuario.Should().Be(conta.IdUsuario);
        result.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        result.Layout.Should().Be(layout);
        result.Periodo.Should().Be(periodo);
        result.Lidas.Should().Be(lidas);
        result.ImportadaEm.Should().Be(importadaEm);
        result.DataVencimentoFatura.Should().BeNull();
        result.MesVencimentoFatura.Should().BeNull();
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_Cartao_Deve_GuardarOUsuarioAOrigemEOVencimentoDaFatura(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        DateOnly vencimento,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoArquivo result = ImportacaoArquivoFactory.Registrar(cartao, layout, periodo, vencimento, lidas, importadaEm);

        result.IdUsuario.Should().Be(cartao.IdUsuario);
        result.Origem.Should().Be(OrigemTransacao.DoCartao(cartao.Id));
        result.DataVencimentoFatura.Should().Be(vencimento);
        result.MesVencimentoFatura.Should().Be((MesReferencia)vencimento);
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_CartaoComLidasNegativas_Deve_LancarExcecao(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        DateOnly vencimento,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoArquivoFactory.Registrar(cartao, layout, periodo, vencimento, lidas: -1, importadaEm);

        registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas");
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_LidasNegativas_Deve_LancarExcecao(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas: -1, importadaEm);

        registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas");
    }

    [Theory, DomainAutoData]
    public void Registrar_Deve_GerarIdVersao7(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoArquivo result = ImportacaoArquivoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.Id.Value.Version.Should().Be(7);
    }
}
