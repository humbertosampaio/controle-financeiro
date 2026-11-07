using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Tests.Shared.Respostas;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class ErroParaModeloTests
{
    public static readonly TheoryData<ErroParaModelo> Erros = new(ErroParaModelo.MesInvalido, ErroParaModelo.UsuarioNaoEncontrado);

    [Theory, MemberData(nameof(Erros))]
    public void Serializar_Deve_TerSomenteAMensagem(ErroParaModelo erro)
    {
        JsonObject json = erro.Serializar();

        json.Select(campo => campo.Key).Should().Equal("mensagem");
        new ErroLido(json).Mensagem.Should().NotBeEmpty();
    }
}
