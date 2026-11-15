using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Despesas;

/// <summary>
/// A saída da conta que provavelmente pagou uma fatura: o valor é exatamente o total dela e a data cai na janela em torno
/// do vencimento. É só uma sugestão, calculada na hora e nunca gravada: a ligação continua explícita, pelo
/// <see cref="Services.PagamentoFaturaService"/>.
/// </summary>
public sealed class CandidatoPagamentoFatura : ValueObject
{
    /// <summary>A saída da conta que pagou a fatura, pelo id: o candidato não carrega o agregado da transação.</summary>
    public IdTransacao IdSaida { get; }

    /// <summary>A fatura que a saída paga, com o vencimento real e o total, igual ao valor da saída.</summary>
    public FaturaCartaoAberta FaturaCartaoAberta { get; }

    /// <summary>O cartão que identifica a fatura ao pagá-la: o de menor id, para a escolha não depender da ordem das listas.</summary>
    public IdCartao IdCartao => FaturaCartaoAberta.Fatura.Cartoes.Min();

    /// <remarks>
    /// Interno porque só o <see cref="Services.CandidatosPagamentoFaturaService"/> garante que a saída e a fatura formam o
    /// único par possível e que a fatura tem cartão.
    /// </remarks>
    internal CandidatoPagamentoFatura(IdTransacao idSaida, FaturaCartaoAberta faturaAberta)
    {
        IdSaida = idSaida;
        FaturaCartaoAberta = faturaAberta;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IdSaida;
        yield return FaturaCartaoAberta;
    }
}
