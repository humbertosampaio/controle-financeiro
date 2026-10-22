using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class GeracaoDespesasRecorrentesServiceTests
{
    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_DespesasRecorrentesSemCartao_Deve_RetornarUmaDespesaSimplesPorRecorrente(
        [Frozen] IdUsuario idUsuario,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Pix);
        List<DespesaRecorrente> recorrentes = fixture.CreateMany<DespesaRecorrente>().ToList();

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes(recorrentes, [], mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(recorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(idUsuario);
            despesa.Should().BeOfType<DespesaSimples>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_DespesasRecorrentesNoCredito_Deve_RetornarUmaDespesaAdiadaPorRecorrente(
        [Frozen] IdUsuario idUsuario,
        [Frozen] Cartao cartao,
        Cartao outroCartao,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        List<DespesaRecorrente> recorrentes = fixture.CreateMany<DespesaRecorrente>().ToList();

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes(
            recorrentes, [outroCartao, cartao], mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(recorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(idUsuario);
            despesa.Should().BeOfType<DespesaAdiada>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_CartaoDaRecorrenteForaDaLista_Deve_LancarExcecao(
        Cartao outroCartao,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        DespesaRecorrente recorrente = fixture.Create<DespesaRecorrente>();

        Action act = () => GeracaoDespesasRecorrentesService.GerarParaMes([recorrente], [outroCartao], mesReferencia);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_SemDespesasRecorrentes_Deve_RetornarListaVazia(MesReferencia mesReferencia)
    {
        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes([], [], mesReferencia);

        result.Should().Succeed()
            .Which.Should().BeEmpty();
    }
}
