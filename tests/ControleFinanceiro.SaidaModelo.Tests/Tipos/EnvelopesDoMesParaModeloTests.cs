using System.Text.Json.Nodes;
using ControleFinanceiro.Application.SaidaModelo;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

/// <summary>Os envelopes que as ferramentas devolvem: o mês e os dados, ou só o erro.</summary>
public sealed class EnvelopesDoMesParaModeloTests
{
    [Theory, UsuarioUnicoAutoData]
    public void SerializarTransacoes_Deve_TerOMesEAsTransacoes(TransacoesDeTeste transacoes, Lancamento lancamento)
    {
        Transacao transacao = transacoes.DaConta(lancamento.SemCategoria());
        MesReferencia mes = transacao.Data;

        JsonObject json = TransacoesDoMesParaModelo.De(mes, [transacao.ParaModelo()]).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "transacoes");
        json.Texto("mes").Should().Be(mes.ToString());
        json["transacoes"]!.AsArray().Should().ContainSingle();
    }

    [Theory, DomainAutoData]
    public void SerializarDespesasFixas_Deve_TerOMesEAsDespesasFixas(DespesaRecorrente recorrente, MesReferencia mes)
    {
        JsonObject json = DespesasFixasDoMesParaModelo.De(mes, [recorrente.ParaModelo(despesaDoMes: null, mes)]).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "despesasFixas");
        json.Texto("mes").Should().Be(mes.ToString());
        json["despesasFixas"]!.AsArray().Should().ContainSingle();
    }

    // Os nomes do JSON divergem das propriedades (TotalPago, TotalPendente, SaldoPrevisto): só o atributo os produz.
    [Theory, DomainAutoData]
    public void SerializarBalanco_Deve_TerOMesEOsTotais(Usuario usuario, MesReferencia mes)
    {
        BalancoMensal balanco = BalancoMensalService.Calcular(usuario, despesasDoMes: [], mes);

        JsonObject json = balanco.ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "rendaLiquida", "pago", "pendente", "saldo");
        json.Texto("mes").Should().Be(balanco.Mes.ToString());
        json["rendaLiquida"]!.GetValue<decimal>().Should().Be(balanco.RendaLiquida);
        json["pago"]!.GetValue<decimal>().Should().Be(balanco.TotalPago);
        json["pendente"]!.GetValue<decimal>().Should().Be(balanco.TotalPendente);
        json["saldo"]!.GetValue<decimal>().Should().Be(balanco.SaldoPrevisto);
    }

    [Fact]
    public void SerializarTransacoes_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = TransacoesDoMesParaModelo.Falha(ErroParaModelo.MesInvalido).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
    }

    [Fact]
    public void SerializarDespesasFixas_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = DespesasFixasDoMesParaModelo.Falha(ErroParaModelo.MesInvalido).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
    }

    [Fact]
    public void SerializarBalanco_Quando_Falha_Deve_TerSoOErro()
    {
        JsonObject json = BalancoMensalParaModelo.Falha(ErroParaModelo.UsuarioNaoEncontrado).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
        json["erro"]!.AsObject().Texto("mensagem").Should().Be(ErroParaModelo.UsuarioNaoEncontrado.Mensagem);
    }
}
