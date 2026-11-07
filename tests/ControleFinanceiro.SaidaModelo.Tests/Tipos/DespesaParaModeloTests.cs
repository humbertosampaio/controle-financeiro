using System.Text.Json.Nodes;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared.Respostas;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class DespesaParaModeloTests
{
    [Theory, DomainAutoData]
    public void Serializar_Deve_TerSomenteOsCamposPermitidos(DespesaSimples despesa)
    {
        JsonObject json = despesa.ParaModelo(despesa.MesReferencia).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "descricao", "dataOcorrencia", "valor", "mes", "status");
    }

    [Theory, DomainAutoData]
    public void Serializar_Deve_LevarOsValoresDaDespesa(DespesaSimples despesa)
    {
        JsonObject json = despesa.ParaModelo(despesa.MesReferencia).Serializar();

        DespesaDoMesLida lida = new(json);
        lida.Id.Should().Be(despesa.Id.Value);
        lida.DataOcorrencia.Should().Be(despesa.DataOcorrencia);
        lida.Valor.Should().Be(despesa.ValorVigente);
        lida.Mes.Should().Be(despesa.MesReferencia.ToString());
        lida.Status.Should().Be(StatusPagamento.Pendente.Name);
    }

    // A primeira parcela nasce paga e a segunda, pendente: o status diz de qual parcela saiu o pagamento.
    [Theory, DomainAutoData]
    public void Serializar_Quando_DespesaParcelada_Deve_LevarOPagamentoDaParcelaDoMes(Fixture fixture)
    {
        fixture.Inject((Parcela)(2, 10));
        DespesaParcelada despesa = fixture.Create<DespesaParcelada>();
        ParcelaDespesa segunda = despesa.Parcelas[1];

        JsonObject json = despesa.ParaModelo(segunda.MesReferencia).Serializar();

        DespesaDoMesLida lida = new(json);
        lida.Valor.Should().Be(segunda.ValorVigente);
        lida.Mes.Should().Be(segunda.MesReferencia.ToString());
        lida.Status.Should().Be(StatusPagamento.Pendente.Name);
    }

    [Theory, DomainAutoData]
    public void ParaModelo_Quando_DespesaSemPagamentoNoMes_Deve_LancarExcecao(DespesaSimples despesa)
    {
        Action act = () => despesa.ParaModelo(despesa.MesReferencia.Posterior());

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesa");
    }

    [Theory]
    [InlineDomainAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineDomainAutoData(FormaDocumento.CnpjPuro)]
    [InlineDomainAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Serializar_Quando_DescricaoDespesaParceladaComDocumento_Deve_SairSemODocumento(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)$"LOJA {documentos.Em(forma)}");
        DespesaParcelada despesa = fixture.Create<DespesaParcelada>();

        JsonObject json = despesa.ParaModelo(despesa.Parcelas[0].MesReferencia).Serializar();

        new DespesaDoMesLida(json).Descricao.Should().Be($"LOJA {DocumentosDeTeste.Rotulo(forma)}");
    }

    [Theory, DomainAutoData]
    public void Serializar_Quando_DescricaoSoComDocumento_Deve_SairVazia(DocumentosDeTeste documentos, Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)documentos.Em(FormaDocumento.CpfPuro));
        DespesaParcelada despesa = fixture.Create<DespesaParcelada>();

        JsonObject json = despesa.ParaModelo(despesa.Parcelas[0].MesReferencia).Serializar();

        new DespesaDoMesLida(json).Descricao.Should().BeEmpty();
    }
}
