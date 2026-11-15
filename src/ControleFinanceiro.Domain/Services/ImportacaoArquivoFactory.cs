using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Registra a importação de um arquivo da conta ou do cartão. O usuário e a origem saem da própria conta ou
/// do próprio cartão, para a importação nunca ficar com o dono de um e a origem de outro.
/// </summary>
public static class ImportacaoArquivoFactory
{
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    public static ImportacaoArquivo Registrar(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ImportacaoArquivo.RegistrarExtrato(conta.IdUsuario, OrigemTransacao.DaConta(conta.Id), layout, periodo, lidas, importadaEm);
    }

    /// <param name="vencimento">A data em que a fatura do arquivo vence.</param>
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    public static ImportacaoArquivo Registrar(
        Cartao cartao,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        DateOnly vencimento,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ImportacaoArquivo.RegistrarFatura(
            cartao.IdUsuario,
            OrigemTransacao.DoCartao(cartao.Id),
            layout,
            periodo,
            vencimento,
            lidas,
            importadaEm);
    }
}
