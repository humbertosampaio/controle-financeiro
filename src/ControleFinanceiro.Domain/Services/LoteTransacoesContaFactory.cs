using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos do extrato de uma conta nas transações do lote, cada uma com a sua chave de
/// deduplicação. O banco da chave sai da conta, e cada transação fica ligada à importação do arquivo. O lote só nasce
/// quando todo texto de beneficiário das saídas já é alias de um beneficiário do usuário, e toda categoria do banco, de
/// uma categoria dele.
/// </summary>
public static class LoteTransacoesContaFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <param name="importacao">A importação do arquivo, da mesma conta.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outra conta, ou uma das listas traz um beneficiário
    /// ou uma categoria de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<TransacaoConta>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoConta> lancamentos)
    {
        if (importacao.IdConta != conta.Id)
            throw new ArgumentException("A importação é de outra conta.", nameof(importacao));

        return LoteTransacoes
            .Conferir(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, conta.IdUsuario, lancamentos)
            .Map(IReadOnlyList<TransacaoConta> () => CriarTransacoes(idUsuario, conta, importacao, lancamentos));
    }

    private static TransacaoConta[] CriarTransacoes(
        IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<LancamentoConta> lancamentos)
    {
        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(conta.Banco, conta.Id, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => TransacaoConta.Criar(idUsuario, importacao.Id, conta.Id, lancamento, chave))
            .ToArray();
    }
}
