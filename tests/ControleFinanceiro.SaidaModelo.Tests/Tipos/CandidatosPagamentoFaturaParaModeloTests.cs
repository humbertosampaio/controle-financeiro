using AutoFixture.Xunit2;
using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;
using System.Text.Json.Nodes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class CandidatosPagamentoFaturaParaModeloTests
{
    // Não saem o final do cartão nem nada da conta: o prever_pagamento_fatura pede só o id de um cartão da fatura.
    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        Lancamento compra,
        Lancamento linhaSaida)
    {
        JsonObject json = cenario.CandidatoPagamentoFaturaUnico(cartao, vencimento, compra, linhaSaida).ParaModelo().Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("transacao", "idCartao", "banco", "mes", "vencimento", "total");
    }

    [Theory, UsuarioUnicoAutoData]
    public void Serializar_Deve_LevarASaidaOCartaoAFaturaEOTotal(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        Lancamento compra,
        Lancamento linhaSaida)
    {
        CandidatoPagamentoFaturaDto candidatoDto = cenario.CandidatoPagamentoFaturaUnico(cartao, vencimento, compra, linhaSaida);

        CandidatoPagamentoFaturaLido lido = new(candidatoDto.ParaModelo().Serializar());

        lido.Transacao.Id.Should().Be(candidatoDto.Saida.Id.Value);
        lido.Transacao.Valor.Should().Be(-100m);
        lido.IdCartao.Should().Be(cartao.Id.Value);
        lido.Banco.Should().Be(cartao.Banco.Name);
        lido.Mes.Should().Be(vencimento.Mes.ToString());
        lido.Vencimento.Should().Be(vencimento);
        lido.Total.Should().Be(100m);
    }

    [Theory, UsuarioUnicoAutoData]
    public void SerializarCandidatos_Deve_TerOMesEOsCandidatos(
        [Frozen] VencimentoFatura vencimento,
        Cartao cartao,
        CenarioPagamentoFatura cenario,
        Lancamento compra,
        Lancamento linhaSaida)
    {
        CandidatoPagamentoFaturaDto candidatoDto = cenario.CandidatoPagamentoFaturaUnico(cartao, vencimento, compra, linhaSaida);

        JsonObject json = CandidatosPagamentoFaturaParaModelo.De(vencimento.Mes, [candidatoDto.ParaModelo()]).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("mes", "candidatos");
        CandidatosPagamentoFaturaLidos lidos = new(json);
        lidos.Mes.Should().Be(vencimento.Mes.ToString());
        lidos.Candidatos.Should().ContainSingle().Which.Transacao.Id.Should().Be(candidatoDto.Saida.Id.Value);
    }

    [Fact]
    public void SerializarCandidatos_Quando_Falha_Deve_TerApenasOErro()
    {
        JsonObject json = CandidatosPagamentoFaturaParaModelo.Falha(ErrosParaModelo.Consulta.MesInvalido).Serializar();

        json.Select(campo => campo.Key).Should().Equal("erro");
    }
}
