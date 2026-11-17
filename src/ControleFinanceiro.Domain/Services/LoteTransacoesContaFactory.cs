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
    /// Quando a importação é de outra conta, um lançamento é de outro tipo de origem, ou uma das listas traz um beneficiário
    /// ou uma categoria de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        if (importacao.IdConta != conta.Id)
            throw new ArgumentException("A importação é de outra conta.", nameof(importacao));

        if (lancamentos.Any(l => l.TipoOrigem != TipoOrigem.Conta))
            throw new ArgumentException("A lista traz um lançamento de outro tipo de origem.", nameof(lancamentos));

        return LoteTransacoes
            .Conferir(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, conta.IdUsuario, lancamentos)
            .Map(IReadOnlyList<Transacao> () => CriarTransacoes(idUsuario, conta, importacao, lancamentos));
    }

    private static Transacao[] CriarTransacoes(
        IdUsuario idUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        OrigemTransacao origem = OrigemTransacao.DaConta(conta.Id);
        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(conta.Banco, origem, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => Transacao.Criar(idUsuario, importacao.Id, origem, mesVencimentoFatura: null, lancamento, chave))
            .ToArray();
    }
}
