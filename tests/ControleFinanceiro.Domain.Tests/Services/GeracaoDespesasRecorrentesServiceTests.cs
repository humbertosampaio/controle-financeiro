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
    public void GerarParaMes_Quando_UsuarioComDespesasRecorrentesValidas_Deve_RetornarListaDeDespesas(
        Usuario usuario,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Pix);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>());

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes(usuario, mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(usuario.DespesasRecorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(usuario.Id);
            despesa.Should().BeOfType<DespesaSimples>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_UsuarioComDespesasRecorrentesCredito_Deve_RetornarListaDeDespesasAdiadas(
        Usuario usuario,
        MesReferencia mesReferencia,
        Fixture fixture)
    {
        fixture.Inject(FormaPagamento.Credito);
        usuario.AdicionarDespesaRecorrente(fixture.Create<DespesaRecorrente>());

        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes(usuario, mesReferencia);

        IReadOnlyList<Despesa> despesas = result.Should().Succeed().Which;
        despesas.Should().HaveSameCount(usuario.DespesasRecorrentes);
        despesas.Should().AllSatisfy(despesa =>
        {
            despesa.IdUsuario.Should().Be(usuario.Id);
            despesa.Should().BeOfType<DespesaAdiada>().Which.MesReferencia.Should().Be(mesReferencia);
        });
    }

    [Theory, DomainAutoData]
    public void GerarParaMes_Quando_UsuarioSemDespesasRecorrentes_Deve_RetornarListaVazia(Usuario usuario, MesReferencia mesReferencia)
    {
        Result<IReadOnlyList<Despesa>> result = GeracaoDespesasRecorrentesService.GerarParaMes(usuario, mesReferencia);

        usuario.DespesasRecorrentes.Should().BeEmpty();
        result.Should().Succeed()
            .Which.Should().BeEmpty();
    }
}
