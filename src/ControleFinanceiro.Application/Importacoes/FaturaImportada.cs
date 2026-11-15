using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O relato da importação de uma fatura, com uma linha por cartão, na ordem em que os finais aparecem no arquivo, para
/// quem a pediu montar a própria resposta. Nada do conteúdo dos lançamentos entra nele.
/// </summary>
/// <param name="Vencimento">O vencimento da fatura, o mesmo para todos os cartões.</param>
/// <param name="PagamentoFaturaAnterior">Falha, com o aviso em texto, quando a fatura não traz o pagamento da anterior.</param>
/// <param name="VencimentoCicloCartoes">
/// Falha, com o aviso em texto, quando o vencimento não fecha com o ciclo cadastrado de algum cartão da fatura.
/// </param>
public sealed record FaturaImportada(
    Banco Banco,
    VencimentoFatura Vencimento,
    IReadOnlyList<FaturaImportada.CartaoImportado> Cartoes,
    Result PagamentoFaturaAnterior,
    Result VencimentoCicloCartoes)
{
    public sealed record CartaoImportado(
        IdImportacao IdImportacao,
        IdCartao IdCartao,
        SufixoCartao Sufixo,
        int Lidas,
        int Novas,
        int JaExistentes,
        int DespesasParceladasCriadas,
        int DespesasAdiadasCriadas);
}
