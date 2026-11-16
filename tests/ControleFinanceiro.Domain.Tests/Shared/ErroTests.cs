using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Tests.Shared;

public sealed class ErroTests
{
    [Theory, AutoData]
    public void ToString_Deve_TrazerOCodigoEADescricao(string codigo, string descricao)
    {
        Erro sut = new UmErro(codigo, descricao);

        sut.ToString().Should().Be($"{codigo}: {descricao}");
    }

    [Theory, AutoData]
    public void Equals_Quando_MesmoTipoEDescricoesDiferentes_Deve_RetornarTrue(string codigo, string descricao, string outraDescricao)
    {
        Erro erro = new UmErro(codigo, descricao);
        Erro outro = new UmErro(codigo, outraDescricao);

        erro.Should().Be(outro);
        erro.GetHashCode().Should().Be(outro.GetHashCode());
    }

    [Theory, AutoData]
    public void Equals_Quando_TiposDiferentesComOMesmoCodigo_Deve_RetornarFalse(string codigo, string descricao)
    {
        Erro erro = new UmErro(codigo, descricao);
        Erro outro = new OutroErro(codigo, descricao);

        erro.Should().NotBe(outro);
    }

    private sealed class UmErro : Erro
    {
        public UmErro(string codigo, string descricao) : base(codigo, descricao)
        {
        }
    }

    private sealed class OutroErro : Erro
    {
        public OutroErro(string codigo, string descricao) : base(codigo, descricao)
        {
        }
    }
}
