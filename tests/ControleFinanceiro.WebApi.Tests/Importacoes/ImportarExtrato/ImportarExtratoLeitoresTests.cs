using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;

namespace ControleFinanceiro.WebApi.Tests.Importacoes.ImportarExtrato;

/// <summary>
/// O endpoint escolhe o leitor pelo formato, e o validator aceita qualquer formato conhecido. Um formato sem leitor
/// no container da API passaria pelo validator e daria 500.
/// </summary>
[Collection(ApiCollection.Nome)]
public sealed class ImportarExtratoLeitoresTests
{
    private readonly ApiComPostgresFixture _api;

    public ImportarExtratoLeitoresTests(ApiComPostgresFixture api)
    {
        _api = api;
    }

    [Fact]
    public void Api_Deve_RegistrarUmLeitorParaCadaFormato()
    {
        IReadOnlyList<ILeitorExtrato> leitores = _api.ObterServicos<ILeitorExtrato>();

        leitores.Select(l => l.Formato).Should().BeEquivalentTo(FormatoExtrato.List);
    }
}
