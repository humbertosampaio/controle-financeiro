using System.Text.Json.Nodes;
using AutoFixture.Xunit2;
using ControleFinanceiro.Application.IA.Saida;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.SaidaModelo.Tests.Tipos;

public sealed class DespesaFixaParaModeloTests
{
    [Theory, RecorrenteSemCartaoAutoData]
    public void Serializar_Quando_SemDespesaDoMes_Deve_TerSomenteOsCamposDaRecorrente(DespesaRecorrente recorrente, MesReferencia mes)
    {
        JsonObject json = recorrente.ParaModelo(despesaDoMes: null, mes).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "descricao", "valor", "diaOcorrencia");
    }

    [Theory, RecorrenteSemCartaoAutoData]
    public void Serializar_Quando_ComDespesaDoMes_Deve_TerOsCamposDaRecorrenteEDaDespesa(DespesaRecorrente recorrente, MesReferencia mes)
    {
        Despesa despesaDoMes = GeracaoDespesasDoMesService.GerarComPagamentoImediato(recorrente, mes).Value;

        JsonObject json = recorrente.ParaModelo(despesaDoMes, mes).Serializar();

        json.Select(campo => campo.Key).Should().BeEquivalentTo("id", "descricao", "valor", "diaOcorrencia", "despesaDoMes");
        json["despesaDoMes"]!.AsObject()
            .Select(campo => campo.Key)
            .Should().BeEquivalentTo("id", "descricao", "dataOcorrencia", "valor", "mes", "status");
    }

    [Theory, RecorrenteSemCartaoAutoData]
    public void Serializar_Deve_LevarOsValoresDaRecorrenteEDaDespesaDoMes(DespesaRecorrente recorrente, MesReferencia mes)
    {
        Despesa despesaDoMes = GeracaoDespesasDoMesService.GerarComPagamentoImediato(recorrente, mes).Value;

        JsonObject json = recorrente.ParaModelo(despesaDoMes, mes).Serializar();

        json["id"]!.GetValue<Guid>().Should().Be(recorrente.Id.Value);
        json["valor"]!.GetValue<decimal>().Should().Be(recorrente.Valor);
        json["diaOcorrencia"]!.GetValue<int>().Should().Be(recorrente.DiaOcorrencia);
        json["despesaDoMes"]!["id"]!.GetValue<Guid>().Should().Be(despesaDoMes.Id.Value);
        json["despesaDoMes"]!.AsObject().Texto("status").Should().Be(StatusPagamento.Pendente.Name);
    }

    [Theory]
    [InlineRecorrenteSemCartaoAutoData(FormaDocumento.CpfComPontuacao)]
    [InlineRecorrenteSemCartaoAutoData(FormaDocumento.CnpjPuro)]
    [InlineRecorrenteSemCartaoAutoData(FormaDocumento.CnpjComLetrasComPontuacao)]
    public void Serializar_Quando_DescricaoComDocumento_Deve_SairSemODocumentoNaRecorrenteENaDespesaDoMes(
        FormaDocumento forma,
        DocumentosDeTeste documentos,
        MesReferencia mes,
        Fixture fixture)
    {
        fixture.Inject((TextoObrigatorio)$"ALUGUEL {documentos.Em(forma)}");
        DespesaRecorrente recorrente = fixture.Create<DespesaRecorrente>();
        Despesa despesaDoMes = GeracaoDespesasDoMesService.GerarComPagamentoImediato(recorrente, mes).Value;

        JsonObject json = recorrente.ParaModelo(despesaDoMes, mes).Serializar();

        string esperada = $"ALUGUEL {DocumentosDeTeste.Rotulo(forma)}";
        json.Texto("descricao").Should().Be(esperada);
        json["despesaDoMes"]!.AsObject().Texto("descricao").Should().Be(esperada);
    }

    /// <summary>A recorrente sem cartão, para a despesa do mês sair da geração com pagamento imediato.</summary>
    private sealed class RecorrenteSemCartaoAutoDataAttribute : DomainAutoDataAttribute
    {
        public RecorrenteSemCartaoAutoDataAttribute()
            : base(fixture => fixture.Inject(FormaPagamento.Pix))
        {
        }
    }

    private sealed class InlineRecorrenteSemCartaoAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineRecorrenteSemCartaoAutoDataAttribute(params object[] valores)
            : base(new RecorrenteSemCartaoAutoDataAttribute(), valores)
        {
        }
    }
}
