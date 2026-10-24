using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class ImportacaoExtratoFactoryTests
{
    [Theory, DomainAutoData]
    public void Registrar_Quando_Conta_Deve_GuardarOsDadosDaImportacaoComOUsuarioEAOrigemDaConta(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoExtrato periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoExtratoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.IdUsuario.Should().Be(conta.IdUsuario);
        result.Origem.Should().Be(OrigemTransacao.DaConta(conta.Id));
        result.Layout.Should().Be(layout);
        result.Periodo.Should().Be(periodo);
        result.Lidas.Should().Be(lidas);
        result.ImportadaEm.Should().Be(importadaEm);
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_Cartao_Deve_GuardarOUsuarioEAOrigemDoCartao(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        PeriodoExtrato periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoExtratoFactory.Registrar(cartao, layout, periodo, lidas, importadaEm);

        result.IdUsuario.Should().Be(cartao.IdUsuario);
        result.Origem.Should().Be(OrigemTransacao.DoCartao(cartao.Id));
    }

    [Theory, DomainAutoData]
    public void Registrar_Quando_LidasNegativas_Deve_LancarExcecao(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoExtrato periodo,
        DateTimeOffset importadaEm)
    {
        Action registrar = () => ImportacaoExtratoFactory.Registrar(conta, layout, periodo, lidas: -1, importadaEm);

        registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas");
    }

    [Theory, DomainAutoData]
    public void Registrar_Deve_GerarIdVersao7(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoExtrato periodo,
        byte lidas,
        DateTimeOffset importadaEm)
    {
        ImportacaoExtrato result = ImportacaoExtratoFactory.Registrar(conta, layout, periodo, lidas, importadaEm);

        result.Id.Value.Version.Should().Be(7);
    }
}
