using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Registra a importação de um arquivo da conta ou do cartão. O usuário e a origem saem da própria conta ou
/// do próprio cartão, para a importação nunca ficar com o dono de um e a origem de outro.
/// </summary>
public static class ImportacaoExtratoFactory
{
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    public static ImportacaoExtrato Registrar(
        ContaBancaria conta,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ImportacaoExtrato.Registrar(conta.IdUsuario, OrigemTransacao.DaConta(conta.Id), formato, periodo, lidas, importadaEm);
    }

    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    public static ImportacaoExtrato Registrar(
        Cartao cartao,
        FormatoExtrato formato,
        PeriodoExtrato periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ImportacaoExtrato.Registrar(cartao.IdUsuario, OrigemTransacao.DoCartao(cartao.Id), formato, periodo, lidas, importadaEm);
    }
}
