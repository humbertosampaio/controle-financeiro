using AutoFixture.Xunit2;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;
using ControleFinanceiro.Tests.Shared;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Tests.Services;

public sealed class VinculoPagamentoServiceTests
{
    /// <summary>A parcela em que a parcelada do fixture nasce: tem parcelas pagas antes dela e pendentes depois.</summary>
    private static readonly Parcela _parcelaDaCompra = (Parcela)(3, 10);

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SimplesESaidaDeConta_Deve_QuitarNaDataDaTransacao(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        ReaisPositivo previsto = despesa.Pagamento.ValorProjetado;
        DateOnly projetada = despesa.Pagamento.DataProjetada;

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.EstaPago.Should().BeTrue();
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
        despesa.Pagamento.DataEfetiva.Should().Be(transacao.Data);
        despesa.Pagamento.DataProjetada.Should().Be(projetada);
        despesa.Pagamento.ValorProjetado.Should().Be(previsto);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ValorDiferenteDoProjetado_Deve_RegistrarOValorDaTransacao(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        ReaisPositivo projetado = despesa.Pagamento.ValorProjetado;

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
        despesa.Pagamento.ValorProjetado.Should().Be(projetado);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaESaidaDoMesmoCartao_Deve_QuitarNoVencimentoProjetado(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.EstaPago.Should().BeTrue();
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
        despesa.Pagamento.DataEfetiva.Should().Be(despesa.Pagamento.DataProjetada);
        despesa.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDaCompra_Deve_QuitarSoAParcelaDoMesmoNumero(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        ParcelaDespesa terceira = despesa.Parcelas[2];
        terceira.Pagamento.IdTransacao.Should().Be(transacao.Id);
        terceira.Pagamento.DataEfetiva.Should().Be(terceira.Pagamento.DataProjetada);
        terceira.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
        despesa.Parcelas.Where(p => p.NumeroParcela != 3).Should().OnlyContain(p => p.Pagamento.IdTransacao == null);
        despesa.Parcelas.Skip(3).Should().OnlyContain(p => !p.Pagamento.EstaPago);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNasceuPagaSemTransacao_Deve_AceitarOVinculoEManterOVencimento(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        ParcelaDespesa primeira = despesa.Parcelas[0];
        primeira.Pagamento.EstaPago.Should().BeTrue();
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 1));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        primeira.Pagamento.IdTransacao.Should().Be(transacao.Id);
        primeira.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
        primeira.Pagamento.DataEfetiva.Should().Be(primeira.Pagamento.DataProjetada);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_TransacaoJaPagaOutraDespesa_Deve_RetornarErroComADespesa(
        DespesaSimples despesa,
        DespesaSimples outra,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, outra);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroTransacaoJaVinculada>()
            .Which.IdDespesa.Should().Be(outra.Id);
        despesa.Pagamento.EstaPago.Should().BeFalse();
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_TransacaoJaPagaEstaDespesa_Deve_RetornarErroComADespesa(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesa);

        result.Should().Fail()
            .Which.Should().BeOfType<ErroTransacaoJaVinculada>()
            .Which.IdDespesa.Should().Be(despesa.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_DespesaJaQuitadaPorOutraTransacao_Deve_RetornarDespesaJaPaga(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao primeira = transacoes.DaConta(saida.AVista());
        VinculoPagamentoService.Vincular(primeira, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DaConta(outraSaida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaPaga.Instancia);
        despesa.Pagamento.IdTransacao.Should().Be(primeira.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaJaQuitadaPorOutraTransacao_Deve_RetornarDespesaJaPaga(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        SaidaDeTeste outraSaida)
    {
        Transacao primeira = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoPagamentoService.Vincular(primeira, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        Transacao outra = transacoes.DoCartao(cartao, outraSaida.DaCompra(despesa, numero: 3));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(outra, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroDespesaJaPaga.Instancia);
    }

    [Theory]
    [InlineVinculoAutoData(50.00)]
    [InlineVinculoAutoData(0.00)]
    public void Vincular_Quando_TransacaoNaoEhSaida_Deve_RetornarErro(
        decimal valor,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.ComValor(valor));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoEhSaida.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SimplesComSaidaDeCartao_Deve_RetornarOrigemIncompativel(
        Cartao cartao,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaComSaidaDeConta_Deve_RetornarOrigemIncompativel(
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_AdiadaComSaidaDeOutroCartao_Deve_RetornarOrigemIncompativel(
        Cartao outroCartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(outroCartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutroCartao_Deve_RetornarOrigemIncompativel(
        Cartao outroCartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(outroCartao, saida.DaCompra(despesa, numero: 3)), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroOrigemIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SaidaDeContaNaParcelada_Deve_RetornarParcelaIncompativel(
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNaSimples_Deve_RetornarParcelaIncompativel(
        Cartao cartao,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.ComParcela());

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaNaAdiada_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.ComParcela());

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_SemParcelaNaParcelada_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(cartao, saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutraData_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia.AddDays(1), despesa.Descricao, (Parcela)(3, despesa.Parcelas.Count));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutraDescricao_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        TextoObrigatorio outraDescricao)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia, outraDescricao, (Parcela)(3, despesa.Parcelas.Count));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_ParcelaDeOutroTotal_Deve_RetornarParcelaIncompativel(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Lancamento compra = saida.DaCompra(despesa.DataOcorrencia, despesa.Descricao, (Parcela)(3, despesa.Parcelas.Count + 2));

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacoes.DoCartao(cartao, compra), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Fail().Which.Should().Be(ErroParcelaIncompativelComDespesa.Instancia);
    }

    [Theory, DomainAutoData]
    public void Vincular_Quando_TransacaoDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada despesa,
        [Frozen] IdUsuario outroUsuario,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        transacao.IdUsuario.Should().Be(outroUsuario);

        Action act = () => VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("transacao");
    }

    [Theory, DomainAutoData]
    public void Vincular_Quando_DespesaQueUsaATransacaoDeOutroUsuario_Deve_LancarExcecao(
        DespesaAdiada deOutroUsuario,
        [Frozen] IdUsuario idUsuario,
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        deOutroUsuario.IdUsuario.Should().NotBe(idUsuario);
        Transacao transacao = transacoes.DaConta(saida.AVista());

        Action act = () => VinculoPagamentoService.Vincular(transacao, despesa, deOutroUsuario);

        act.Should().ThrowExactly<ArgumentException>().WithParameterName("despesaQueUsaATransacao");
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_SimplesVinculada_Deve_VoltarParaPendente(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento original = despesa.Pagamento;
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(transacao.Id);

        result.Should().Succeed();
        despesa.Pagamento.Should().Be(original);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_AdiadaVinculada_Deve_VoltarParaPendente(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento original = despesa.Pagamento;
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(transacao.Id);

        result.Should().Succeed();
        despesa.Pagamento.Should().Be(original);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_ParcelaVinculada_Deve_VoltarSoElaParaPendente(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Pagamento[] originais = despesa.Parcelas.Select(p => p.Pagamento).ToArray();
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(transacao.Id);

        result.Should().Succeed();
        despesa.Parcelas.Select(p => p.Pagamento).Should().Equal(originais);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_ParcelaNasceuPaga_Deve_LevarAPendente(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 1));
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(transacao.Id);

        result.Should().Succeed();
        Pagamento pagamento = despesa.Parcelas[0].Pagamento;
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.DataEfetiva.Should().BeNull();
        pagamento.ValorEfetivo.Should().BeNull();
        pagamento.IdTransacao.Should().BeNull();
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_InteiraSemAquelaTransacao_Deve_RetornarNaoVinculada(DespesaSimples despesa, IdTransacao idTransacao)
    {
        UnitResult<Erro> result = despesa.DesfazerVinculo(idTransacao);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoVinculada.Instancia);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_ParceladaSemAquelaTransacao_Deve_RetornarNaoVinculada(DespesaParcelada despesa, IdTransacao idTransacao)
    {
        UnitResult<Erro> result = despesa.DesfazerVinculo(idTransacao);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoVinculada.Instancia);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_InteiraPagaPorOutraTransacao_Deve_RetornarNaoVinculadaEContinuarPaga(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        IdTransacao outraTransacao)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(outraTransacao);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoVinculada.Instancia);
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, VinculoAutoData]
    public void DesfazerVinculo_Quando_ParcelaPagaPorOutraTransacao_Deve_RetornarNaoVinculadaEContinuarPaga(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        IdTransacao outraTransacao)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        UnitResult<Erro> result = despesa.DesfazerVinculo(outraTransacao);

        result.Should().Fail().Which.Should().Be(ErroTransacaoNaoVinculada.Instancia);
        despesa.Parcelas[2].Pagamento.IdTransacao.Should().Be(transacao.Id);
    }

    [Theory, VinculoAutoData]
    public void Vincular_Quando_InteiraNasceuPagaSemTransacao_Deve_AceitarOVinculo(
        DespesaSimples modelo,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        // O fixture entrega a despesa pendente: aqui a mesma despesa nasce paga, pelo Criar.
        DespesaSimples despesa = DespesaSimples
            .Criar(modelo.IdUsuario, modelo.IdBeneficiario, modelo.Descricao, modelo.IdCategoria, modelo.DataOcorrencia, modelo.FormaPagamento, modelo.Pagamento.ValorProjetado)
            .Value;
        Transacao transacao = transacoes.DaConta(saida.AVista());

        UnitResult<Erro> result = VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None);

        result.Should().Succeed();
        despesa.Pagamento.IdTransacao.Should().Be(transacao.Id);
        despesa.Pagamento.ValorEfetivo.Should().Be(saida.ValorPago);
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_SimplesQuitadaPelaTransacao_Deve_RetornarOMesDaDespesa(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DaConta(saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<MesReferencia> result = despesa.MesQuitado(transacao.Id);

        result.Should().HaveValue(despesa.MesReferencia);
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_AdiadaQuitadaPelaTransacao_Deve_RetornarOMesDaDespesa(
        [Frozen] Cartao cartao,
        DespesaAdiada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.AVista());
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<MesReferencia> result = despesa.MesQuitado(transacao.Id);

        result.Should().HaveValue(despesa.MesReferencia);
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_InteiraPendente_Deve_RetornarVazio(DespesaSimples despesa, IdTransacao idTransacao)
    {
        Maybe<MesReferencia> result = despesa.MesQuitado(idTransacao);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_InteiraQuitadaPorOutraTransacao_Deve_RetornarVazio(
        DespesaSimples despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida,
        IdTransacao outraTransacao)
    {
        VinculoPagamentoService.Vincular(transacoes.DaConta(saida.AVista()), despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<MesReferencia> result = despesa.MesQuitado(outraTransacao);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_ParcelaQuitadaPelaTransacao_Deve_RetornarOMesDaParcela(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();

        Maybe<MesReferencia> result = despesa.MesQuitado(transacao.Id);

        result.Should().HaveValue(despesa.Parcelas[2].MesReferencia);
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_TransacaoNaoQuitouParcela_Deve_RetornarVazio(DespesaParcelada despesa, IdTransacao idTransacao)
    {
        Maybe<MesReferencia> result = despesa.MesQuitado(idTransacao);

        result.Should().HaveNoValue();
    }

    [Theory, VinculoAutoData]
    public void MesQuitado_Quando_VinculoDaParcelaDesfeito_Deve_RetornarVazio(
        [Frozen] Cartao cartao,
        DespesaParcelada despesa,
        TransacoesDeTeste transacoes,
        SaidaDeTeste saida)
    {
        Transacao transacao = transacoes.DoCartao(cartao, saida.DaCompra(despesa, numero: 3));
        VinculoPagamentoService.Vincular(transacao, despesa, despesaQueUsaATransacao: Maybe<Despesa>.None).Should().Succeed();
        despesa.DesfazerVinculo(transacao.Id).Should().Succeed();

        Maybe<MesReferencia> result = despesa.MesQuitado(transacao.Id);

        result.Should().HaveNoValue();
    }

    /// <summary>Uma saída de dinheiro e os lançamentos que os testes derivam dela, com o valor nunca zerado.</summary>
    public sealed class SaidaDeTeste
    {
        private readonly DateOnly _data;
        private readonly decimal _valor;
        private readonly TextoObrigatorio _descricao;
        private readonly IdentidadeLancamento _identidade;

        public ReaisPositivo ValorPago => (ReaisPositivo)decimal.Abs(_valor);

        public SaidaDeTeste(DateOnly data, ReaisPositivo valor, TextoObrigatorio descricao, IdentidadeLancamento identidade)
        {
            _data = data;
            _valor = -(valor + 1m);
            _descricao = descricao;
            _identidade = identidade;
        }

        public Lancamento AVista() => Criar(_data, _valor, _descricao, parcela: null);

        public Lancamento ComParcela() => Criar(_data, _valor, _descricao, _parcelaDaCompra);

        public Lancamento ComValor(decimal valor) => Criar(_data, valor, _descricao, parcela: null);

        public Lancamento DaCompra(DespesaParcelada despesa, int numero) =>
            Criar(despesa.DataOcorrencia, _valor, despesa.Descricao, (Parcela)(numero, despesa.Parcelas.Count));

        /// <summary>Uma parcela com a chave escolhida pelo teste, para os casos de outra compra.</summary>
        public Lancamento DaCompra(DateOnly data, TextoObrigatorio descricao, Parcela parcela) => Criar(data, _valor, descricao, parcela);

        private Lancamento Criar(DateOnly data, decimal valor, TextoObrigatorio descricao, Parcela? parcela) =>
            Lancamento.DoCartao(data, hora: null, valor, descricao, contraparte: null, parcela, categoriaBanco: null, _identidade);
    }

    /// <summary>
    /// Tudo sai do mesmo usuário, e a parcelada, na parcela 3 de 10, que tem parcelas pagas e pendentes.
    /// </summary>
    private sealed class VinculoAutoDataAttribute : DomainAutoDataAttribute
    {
        public VinculoAutoDataAttribute()
            : base(fixture =>
            {
                fixture.Freeze<IdUsuario>();
                fixture.Register(() => _parcelaDaCompra);
            })
        {
        }
    }

    private sealed class InlineVinculoAutoDataAttribute : InlineAutoDataAttribute
    {
        public InlineVinculoAutoDataAttribute(params object[] valores)
            : base(new VinculoAutoDataAttribute(), valores)
        {
        }
    }
}
