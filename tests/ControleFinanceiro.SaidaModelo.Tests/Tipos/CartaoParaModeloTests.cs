using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class CartaoParaModeloTests
{
    // Sai o final, e nunca o número inteiro: o cartão só guarda os 4 últimos dígitos.
    [Theory, DomainAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(Cartao cartao)
    {
        JsonObject json = cartao.ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "banco", "final", "diaFechamento", "diaVencimento");
    }

    [Theory, DomainAutoData]
    public void Serializar_Deve_LevarOIdOBancoOFinalEOsDias(Cartao cartao)
    {
        JsonObject json = cartao.ParaModelo().Serializar();

        CartaoLido lido = new(json);
        lido.Id.Should().Be(cartao.Id.Value);
        lido.Banco.Should().Be(cartao.Banco.Name);
        lido.Final.Should().Be(cartao.Sufixo);
        lido.DiaFechamento.Should().Be(cartao.DiaFechamento);
        lido.DiaVencimento.Should().Be(cartao.DiaVencimento);
    }

    [Theory, DomainAutoData]
    public void SerializarCartoes_Deve_TerSoOsCartoes(Cartao cartao)
    {
        JsonObject json = CartoesParaModelo.De([cartao.ParaModelo()]).Serializar();

        json.Select(campo => campo.Key).Should().Equal("cartoes");
        new CartoesLidos(json).Cartoes.Should().ContainSingle().Which.Id.Should().Be(cartao.Id.Value);
    }
}
