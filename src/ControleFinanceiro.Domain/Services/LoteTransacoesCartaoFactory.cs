using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos da fatura de um cartão nas transações do lote, cada uma com a sua chave de
/// deduplicação e o mês de vencimento da fatura. O banco da chave sai do cartão, e cada transação fica ligada à
/// importação do arquivo. O lote só nasce quando todo texto de beneficiário das saídas já é alias de um beneficiário do
/// usuário, e toda categoria do banco, de uma categoria dele.
/// </summary>
public static class LoteTransacoesCartaoFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <param name="importacao">A importação do arquivo, do mesmo cartão.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outro cartão, ou uma das listas traz um beneficiário
    /// ou uma categoria de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<TransacaoCartao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        Cartao cartao,
        ImportacaoFatura importacao,
        IReadOnlyList<LancamentoCartao> lancamentos)
    {
        if (importacao.IdCartao != cartao.Id)
            throw new ArgumentException("A importação é de outro cartão.", nameof(importacao));

        return LoteTransacoes
            .Conferir(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, cartao.IdUsuario, lancamentos)
            .Map(IReadOnlyList<TransacaoCartao> () => CriarTransacoes(idUsuario, cartao, importacao, lancamentos));
    }

    private static TransacaoCartao[] CriarTransacoes(
        IdUsuario idUsuario,
        Cartao cartao,
        ImportacaoFatura importacao,
        IReadOnlyList<LancamentoCartao> lancamentos)
    {
        OrigemTransacao origem = OrigemTransacao.DoCartao(cartao.Id);
        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(cartao.Banco, origem, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => TransacaoCartao.Criar(idUsuario, importacao.Id, cartao.Id, importacao.MesVencimento, lancamento, chave))
            .ToArray();
    }
}
