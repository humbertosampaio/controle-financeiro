using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class PeriodoArquivoTests
{
    [Theory]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(30)]
    public void Criar_Quando_FimNoInicioOuDepois_Deve_RetornarSucesso(int diasAlemDoInicio, DateOnly inicio)
    {
        DateOnly fim = inicio.AddDays(diasAlemDoInicio);

        Result<PeriodoArquivo> result = PeriodoArquivo.Criar(inicio, fim);

        PeriodoArquivo periodo = result.Should().Succeed().Subject;
        periodo.Inicio.Should().Be(inicio);
        periodo.Fim.Should().Be(fim);
    }

    [Theory, DomainAutoData]
    public void Criar_Quando_FimAntesDoInicio_Deve_RetornarFalha(DateOnly inicio)
    {
        Result<PeriodoArquivo> result = PeriodoArquivo.Criar(inicio, inicio.AddDays(-1));

        result.Should().FailWith("O início do período não pode ser depois do fim.");
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Quando_FimNoInicioOuDepois_Deve_RetornarInstancia(DateOnly inicio)
    {
        PeriodoArquivo result = (PeriodoArquivo)(inicio, inicio.AddDays(1));

        result.Inicio.Should().Be(inicio);
        result.Fim.Should().Be(inicio.AddDays(1));
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Quando_FimAntesDoInicio_Deve_LancarExcecao(DateOnly inicio)
    {
        Func<PeriodoArquivo> act = () => (PeriodoArquivo)(inicio, inicio.AddDays(-1));

        act.Should().ThrowExactly<ResultFailureException>();
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_DataNasBordas_Deve_RetornarTrue(PeriodoArquivo sut)
    {
        sut.Contem(sut.Inicio).Should().BeTrue();
        sut.Contem(sut.Fim).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void Contem_Quando_DataForaDoPeriodo_Deve_RetornarFalse(PeriodoArquivo sut)
    {
        sut.Contem(sut.Inicio.AddDays(-1)).Should().BeFalse();
        sut.Contem(sut.Fim.AddDays(1)).Should().BeFalse();
    }
}
