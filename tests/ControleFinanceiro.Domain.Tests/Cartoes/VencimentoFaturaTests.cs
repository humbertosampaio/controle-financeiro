using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Cartoes;

public sealed class VencimentoFaturaTests
{
    [Fact]
    public void Projetado_Deve_SerODiaDeVencimentoNoMes()
    {
        VencimentoFatura result = VencimentoFatura.Projetado((MesReferencia)(02, 2026), (DiaVencimentoCartao)24);

        result.Value.Should().Be(new DateOnly(2026, 02, 24));
    }

    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarADataExata(DateOnly data)
    {
        VencimentoFatura result = VencimentoFatura.Criar(data);

        result.Value.Should().Be(data);
    }

    [Theory, DomainAutoData]
    public void OperadorExplicito_Deve_SerODaDataExata(DateOnly data)
    {
        VencimentoFatura result = (VencimentoFatura)data;

        result.Should().Be(VencimentoFatura.Criar(data));
    }

    [Theory, DomainAutoData]
    public void Mes_Deve_SerOMesDaData(DateOnly data)
    {
        VencimentoFatura.Criar(data).Mes.Should().Be((MesReferencia)data);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_DataExataIgualAoProjetado_Deve_SerIgual(MesReferencia mes, DiaVencimentoCartao dia)
    {
        VencimentoFatura projetado = VencimentoFatura.Projetado(mes, dia);

        VencimentoFatura.Criar(mes.NoDia(dia)).Should().Be(projetado);
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_MesmaData_Deve_SerFalso(DateOnly data)
    {
        VencimentoFatura sut = VencimentoFatura.Criar(data);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Criar(data)).Should().BeFalse();
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_OutraDataNoMesmoMes_Deve_SerVerdadeiro(
        MesReferencia mes,
        DiaVencimentoCartao dia,
        DiaVencimentoCartao outroDia)
    {
        VencimentoFatura sut = VencimentoFatura.Projetado(mes, dia);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Projetado(mes, outroDia)).Should().BeTrue();
    }

    [Theory, DomainAutoData]
    public void EhOutraDataNoMesmoMes_Quando_OutroMes_Deve_SerFalso(MesReferencia mes, MesReferencia outroMes, DiaVencimentoCartao dia)
    {
        VencimentoFatura sut = VencimentoFatura.Projetado(mes, dia);

        sut.EhOutraDataNoMesmoMes(VencimentoFatura.Projetado(outroMes, dia)).Should().BeFalse();
    }

    [Fact]
    public void Ordem_Deve_SerADaData()
    {
        VencimentoFatura diaMaiorNoMesAnterior = VencimentoFatura.Projetado((MesReferencia)(03, 2026), (DiaVencimentoCartao)24);
        VencimentoFatura diaMenorNoMesSeguinte = VencimentoFatura.Projetado((MesReferencia)(04, 2026), (DiaVencimentoCartao)1);
        VencimentoFatura diaMaiorNoMesSeguinte = VencimentoFatura.Criar(new DateOnly(2026, 04, 13));

        VencimentoFatura[] result = new[] { diaMaiorNoMesSeguinte, diaMaiorNoMesAnterior, diaMenorNoMesSeguinte }.Order().ToArray();

        result.Should().Equal(diaMaiorNoMesAnterior, diaMenorNoMesSeguinte, diaMaiorNoMesSeguinte);
    }

    [Theory]
    [InlineDomainAutoData(-VencimentoFatura.DiasAntesVencimento)]
    [InlineDomainAutoData(0)]
    [InlineDomainAutoData(VencimentoFatura.DiasDepoisVencimento)]
    public void AceitaPagamentoEm_Quando_DentroDaJanela_Deve_SerVerdadeiro(int diasVencimento, VencimentoFatura sut)
    {
        sut.AceitaPagamentoEm(sut.Value.AddDays(diasVencimento)).Should().BeTrue();
    }

    [Theory]
    [InlineDomainAutoData(-VencimentoFatura.DiasAntesVencimento - 1)]
    [InlineDomainAutoData(VencimentoFatura.DiasDepoisVencimento + 1)]
    public void AceitaPagamentoEm_Quando_ForaDaJanela_Deve_SerFalso(int diasVencimento, VencimentoFatura sut)
    {
        sut.AceitaPagamentoEm(sut.Value.AddDays(diasVencimento)).Should().BeFalse();
    }

    // As datas são o assunto: com 7 dias para cada lado, 08/03 − 7 e 24/03 + 7 ainda caem em março.
    [Fact]
    public void MesesVencimentoFaturasPagaveisEm_Quando_JanelaDentroDoMes_Deve_SerApenasOMes()
    {
        Periodo periodoPagamentos = new Periodo(new DateOnly(2026, 3, 8), new DateOnly(2026, 3, 24));

        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesVencimentoFaturasPagaveisEm(periodoPagamentos);

        result.Should().Equal((MesReferencia)(3, 2026));
    }

    // 07/03 − 7 cai em 28/02.
    [Fact]
    public void MesesVencimentoFaturasPagaveisEm_Quando_InicioMenosDiasDepoisNoMesAnterior_Deve_ComecarNoMesAnterior()
    {
        Periodo periodoPagamentos = new Periodo(new DateOnly(2026, 3, 7), new DateOnly(2026, 3, 24));

        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesVencimentoFaturasPagaveisEm(periodoPagamentos);

        result.Should().Equal((MesReferencia)(2, 2026), (MesReferencia)(3, 2026));
    }

    // 25/03 + 7 cai em 01/04.
    [Fact]
    public void MesesVencimentoFaturasPagaveisEm_Quando_FimMaisDiasAntesNoMesSeguinte_Deve_TerminarNoMesSeguinte()
    {
        Periodo periodoPagamentos = new Periodo(new DateOnly(2026, 3, 8), new DateOnly(2026, 3, 25));

        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesVencimentoFaturasPagaveisEm(periodoPagamentos);

        result.Should().Equal((MesReferencia)(3, 2026), (MesReferencia)(4, 2026));
    }

    [Theory]
    [InlineData(2026, 12, 10, 2026, 12, 28)]
    [InlineData(2027, 1, 5, 2027, 1, 20)]
    public void MesesVencimentoFaturasPagaveisEm_Quando_JanelaViraOAno_Deve_IrDeDezembroAJaneiro(
        int anoInicio,
        int mesInicio,
        int diaInicio,
        int anoFim,
        int mesFim,
        int diaFim)
    {
        Periodo periodoPagamentos = new Periodo(new DateOnly(anoInicio, mesInicio, diaInicio), new DateOnly(anoFim, mesFim, diaFim));

        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesVencimentoFaturasPagaveisEm(periodoPagamentos);

        result.Should().Equal((MesReferencia)(12, 2026), (MesReferencia)(1, 2027));
    }

    // As saídas que pagam uma fatura de março vão de 22/02 a 07/04, e as faturas que elas podem pagar vencem de fevereiro a abril.
    [Fact]
    public void MesesVencimentoFaturasConcorrentes_Deve_IrDoMesAnteriorAoSeguinte()
    {
        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesVencimentoFaturasConcorrentes((MesReferencia)(3, 2026));

        result.Should().Equal((MesReferencia)(2, 2026), (MesReferencia)(3, 2026), (MesReferencia)(4, 2026));
    }

    // Com 7 dias para cada lado, o vencimento de 01/03 aceita pagamento desde 22/02, e o de 31/03, até 07/04.
    [Fact]
    public void MesesPagamentoFaturasVencendoEm_Deve_IrDoMesAnteriorAoSeguinte()
    {
        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesPagamentoFaturasVencendoEm((MesReferencia)(3, 2026));

        result.Should().Equal((MesReferencia)(2, 2026), (MesReferencia)(3, 2026), (MesReferencia)(4, 2026));
    }

    [Fact]
    public void MesesPagamentoFaturasVencendoEm_Quando_Janeiro_Deve_ComecarEmDezembroDoAnoAnterior()
    {
        IReadOnlyList<MesReferencia> result = VencimentoFatura.MesesPagamentoFaturasVencendoEm((MesReferencia)(1, 2027));

        result.Should().Equal((MesReferencia)(12, 2026), (MesReferencia)(1, 2027), (MesReferencia)(2, 2027));
    }

    // Cada mês de pagamento devolvido aceita uma data de pagamento de alguma fatura que vence no mês pedido.
    [Fact]
    public void MesesPagamentoFaturasVencendoEm_Deve_CobrirAJanelaDeCadaDiaDoMes()
    {
        MesReferencia mesVencimento = (MesReferencia)(3, 2026);
        IEnumerable<MesReferencia> mesesJanelas = Enumerable
            .Range(start: 0, count: mesVencimento.Fim.DayNumber - mesVencimento.Inicio.DayNumber + 1)
            .Select(dias => VencimentoFatura.Criar(mesVencimento.Inicio.AddDays(dias)))
            .SelectMany(vencimento => new MesReferencia[]
            {
                vencimento.Value.AddDays(-VencimentoFatura.DiasAntesVencimento),
                vencimento.Value.AddDays(VencimentoFatura.DiasDepoisVencimento),
            })
            .Distinct();

        VencimentoFatura.MesesPagamentoFaturasVencendoEm(mesVencimento).Should().BeEquivalentTo(mesesJanelas);
    }
}
