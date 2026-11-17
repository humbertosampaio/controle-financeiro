using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Uma transação vinda da fatura de um cartão: a compra, à vista ou parcelada, o estorno, a tarifa ou o pagamento da fatura anterior.</summary>
public sealed class TransacaoCartao : Transacao
{
    public IdCartao IdCartao { get; private set; }

    /// <summary>
    /// O mês em que vence a fatura que trouxe a transação, copiado da importação que a criou. Uma reimportação com outro
    /// vencimento não o muda.
    /// </summary>
    public MesReferencia MesVencimentoFatura { get; private set; } = null!;

    /// <summary>Preenchida quando o banco informou a parcela; nula na compra à vista.</summary>
    public Parcela? Parcela { get; private set; }

    /// <summary>Verdadeiro quando a transação é uma parcela de uma compra parcelada.</summary>
    public bool EhParcelada => Parcela is not null;

    /// <summary>Verdadeiro quando a transação é uma saída parcelada, a única que pode pagar uma parcela.</summary>
    public bool EhParcelaPaga => EhSaida && EhParcelada;

    /// <summary>
    /// Verdadeiro quando a transação é a entrada que pagou a fatura anterior, copiado do
    /// <see cref="LancamentoCartao.EhPagamentoFaturaAnterior"/>. A transação importada antes da marca fica falsa.
    /// </summary>
    public bool EhPagamentoFaturaAnterior { get; private set; }

    /// <summary>A descrição na saída, pela regra do <see cref="LancamentoCartao.TextoBeneficiario"/>.</summary>
    public override string? TextoBeneficiario => LancamentoCartao.TextoBeneficiarioDe(EhSaida, Descricao);

    /// <summary>
    /// Verdadeiro na saída sem parcela. A linha sem texto de beneficiário fica de fora porque a despesa exige um
    /// beneficiário, como no <see cref="LancamentoCartao.EhCompraParcelada"/>. O pagamento da fatura anterior e o estorno
    /// chegam como entrada e também ficam de fora.
    /// </summary>
    public bool EhCompraAVista => EhSaida && !EhParcelada && AliasTextoBeneficiario is not null;

    private TransacaoCartao()
    {
    }

    private TransacaoCartao(
        IdTransacao id,
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        IdCartao idCartao,
        MesReferencia mesVencimentoFatura,
        LancamentoCartao lancamento,
        ChaveDeduplicacao chave)
            : base(id, idUsuario, idImportacao, lancamento, chave)
    {
        IdCartao = idCartao;
        MesVencimentoFatura = mesVencimentoFatura;
        Parcela = lancamento.Parcela;
        EhPagamentoFaturaAnterior = lancamento.EhPagamentoFaturaAnterior;
    }

    /// <summary>
    /// Verdadeiro na linha que entra no total da <paramref name="fatura"/>: de um cartão dela, trazida pela fatura do mês
    /// de vencimento dela (o <see cref="MesVencimentoFatura"/>), e que não é o pagamento da fatura anterior. Entram as
    /// compras, as parcelas, os estornos e as tarifas.
    /// </summary>
    public bool CompoeTotalFatura(FaturaCartao fatura)
    {
        return fatura.Contem(IdCartao)
            && MesVencimentoFatura == fatura.Vencimento.Mes
            && !EhPagamentoFaturaAnterior;
    }

    /// <remarks>
    /// Interno pois a <paramref name="chave"/> precisa vir de <see cref="ChaveDeduplicacao.CalcularLote(Shared.ValueObjects.Banco, Cartoes.IdCartao, IEnumerable{IdentidadeLancamento})"/>, com o mesmo cartão e o lote do
    /// lançamento, o <paramref name="idUsuario"/> precisa ser o dono do cartão e a importação precisa ser do mesmo cartão, e só a
    /// <see cref="Services.LoteTransacoesCartaoFactory"/> garante isso.
    /// </remarks>
    /// <param name="mesVencimentoFatura">O mês da fatura da importação.</param>
    internal static TransacaoCartao Criar(
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        IdCartao idCartao,
        MesReferencia mesVencimentoFatura,
        LancamentoCartao lancamento,
        ChaveDeduplicacao chave)
    {
        return new TransacaoCartao(IdTransacao.FromNewVersion7Guid(), idUsuario, idImportacao, idCartao, mesVencimentoFatura, lancamento, chave);
    }
}
