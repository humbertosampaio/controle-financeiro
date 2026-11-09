using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaAdiada : DespesaInteira, IDespesaDoCartao
{
    public IdCartao IdCartao { get; private set; }

    /// <summary>A compra da fatura que cobrou a despesa, ou nulo enquanto nenhuma a cobrou.</summary>
    public Cobranca? Cobranca { get; private set; }

    private DespesaAdiada()
    {
    }

    private DespesaAdiada(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        IdCartao idCartao,
        IdDespesaRecorrente? idDespesaRecorrente,
        Pagamento pagamento)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            idDespesaRecorrente,
            pagamento)
    {
        IdCartao = idCartao;
    }

    /// <param name="vencimento">O vencimento da fatura em que a ocorrência cai.</param>
    /// <param name="idDespesaRecorrente">A recorrente que gerou a despesa, ou nulo quando ela não veio de uma.</param>
    /// <exception cref="ArgumentException">Quando o mês de vencimento vem antes do mês da ocorrência.</exception>
    public static DespesaAdiada Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        IdCartao idCartao,
        VencimentoFatura vencimento,
        IdDespesaRecorrente? idDespesaRecorrente)
    {
        MesReferencia mesOcorrencia = dataOcorrencia;
        if (vencimento.Mes < mesOcorrencia)
            throw new ArgumentException("O mês de vencimento vem antes do mês da ocorrência.", nameof(vencimento));

        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada: vencimento);

        return new DespesaAdiada(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito,
            idCartao,
            idDespesaRecorrente,
            pagamento);
    }

    /// <summary>
    /// A despesa da compra à vista que a fatura trouxe sem despesa prevista: nasce cobrada pela compra, com o valor dela, e
    /// pendente até a saída da conta pagar a fatura.
    /// </summary>
    /// <remarks>Interno porque só a <see cref="Services.DespesaAdiadaFactory"/> confere a compra e a previsão.</remarks>
    internal static DespesaAdiada CriarCobrada(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        IdCartao idCartao,
        VencimentoFatura vencimento,
        Cobranca cobranca)
    {
        DespesaAdiada despesa = Criar(
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            cobranca.Valor,
            idCartao,
            vencimento,
            idDespesaRecorrente: null);

        despesa.Cobranca = cobranca;
        return despesa;
    }

    /// <summary>
    /// Verdadeiro quando a despesa prevê a compra do beneficiário na fatura: ainda sem cobrança, e por isso pendente, no
    /// mês de vencimento dela e num dos cartões dela, para a recorrente prevista num final e cobrada noutro.
    /// </summary>
    public bool EhPrevisaoDe(FaturaCartao fatura, IdBeneficiario idBeneficiario)
    {
        return Cobranca is null
            && IdBeneficiario == idBeneficiario
            && MesReferencia == fatura.Vencimento.Mes
            && fatura.Contem(IdCartao);
    }

    /// <summary>O efetivo quando paga, o cobrado quando a fatura a cobrou, senão o projetado.</summary>
    public override ReaisPositivo ValorVigente => Pagamento.ValorVigenteCom(Cobranca);

    /// <summary>
    /// Interno porque só o <see cref="Services.VinculoTransacaoService"/> confere a transação. Cobrar não paga: o
    /// pagamento continua como está.
    /// </summary>
    internal UnitResult<Erro> CobrarCom(Cobranca cobranca)
    {
        if (Cobranca is not null)
            return ErroDespesaJaCobrada.Instancia;

        Cobranca = cobranca;
        return UnitResult.Success<Erro>();
    }

    /// <summary>
    /// Interno porque só o <see cref="Services.VinculoTransacaoService"/> confere a origem da transação. A despesa já paga
    /// pela fatura falha com <see cref="ErroDespesaJaPaga"/>.
    /// </summary>
    internal UnitResult<Erro> DesfazerCobranca(IdTransacao idTransacao)
    {
        if (Cobranca?.IdTransacao != idTransacao)
            return ErroTransacaoNaoCobrouADespesa.Instancia;

        if (Pagamento.IdTransacao is not null)
            return ErroDespesaJaPaga.Instancia;

        Cobranca = null;
        return UnitResult.Success<Erro>();
    }

    public bool FoiPagaSoPelaFatura(FaturaCartao fatura, IdTransacao idSaida)
    {
        return fatura.Contem(IdCartao) && (Pagamento.IdTransacao != idSaida || MesReferencia == fatura.Vencimento.Mes);
    }

    bool IDespesaDoCartao.PagarFatura(FaturaCartao fatura, IdTransacao idSaida, DateOnly dataPagamento)
    {
        if (!fatura.Contem(IdCartao) || MesReferencia != fatura.Vencimento.Mes)
            return false;

        Maybe<Pagamento> pago = Pagamento.PagarPelaFatura(Cobranca, idSaida, dataPagamento);
        if (pago.HasNoValue)
            return false;

        Pagamento = pago.Value;
        return true;
    }

    UnitResult<Erro> IDespesaDoCartao.DesfazerPagamento(IdTransacao idSaida) => Pagamento.Reabrir(idSaida).Map(p => Pagamento = p);

    public override Maybe<Pagamento> PagamentoCobradoPor(IdTransacao idTransacao) =>
        Cobranca?.IdTransacao == idTransacao ? Pagamento : Maybe<Pagamento>.None;
}
