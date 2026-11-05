using System.Text.Json.Nodes;
using ControleFinanceiro.Application.SaidaModelo;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class ErroParaModeloTests
{
    public static readonly TheoryData<ErroParaModelo> Erros = new(ErroParaModelo.MesInvalido, ErroParaModelo.UsuarioNaoEncontrado);

    [Theory, MemberData(nameof(Erros))]
    public void Serializar_Deve_TerSomenteAMensagem(ErroParaModelo erro)
    {
        JsonObject json = erro.Serializar();

        json.Select(campo => campo.Key).Should().Equal("mensagem");
        json.Texto("mensagem").Should().NotBeEmpty();
    }
}
