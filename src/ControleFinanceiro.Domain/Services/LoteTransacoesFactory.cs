using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Transforma os lançamentos lidos de um extrato ou de uma fatura nas transações do lote, cada uma com a
/// sua chave de deduplicação. A origem e o banco da chave saem da conta ou do cartão, e cada transação fica
/// ligada à importação do arquivo. O lote só nasce quando todo texto de beneficiário das saídas já é alias de um
/// beneficiário do usuário.
/// </summary>
public static class LoteTransacoesFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="importacao">A importação do arquivo, da mesma conta.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outra origem, ou a lista traz um beneficiário de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        ContaBancaria conta,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        return Criar(idUsuario, beneficiariosDoUsuario, conta.IdUsuario, conta.Banco, OrigemTransacao.DaConta(conta.Id), importacao, lancamentos);
    }

    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="importacao">A importação do arquivo, do mesmo cartão.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outra origem, ou a lista traz um beneficiário de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        Cartao cartao,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        return Criar(idUsuario, beneficiariosDoUsuario, cartao.IdUsuario, cartao.Banco, OrigemTransacao.DoCartao(cartao.Id), importacao, lancamentos);
    }

    /// <summary>
    /// Os problemas do próprio arquivo, como o identificador repetido, aparecem antes dos beneficiários não resolvidos.
    /// </summary>
    private static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IdUsuario donoDaOrigem,
        Banco banco,
        OrigemTransacao origem,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        if (donoDaOrigem != idUsuario)
            return ErroOrigemNaoPertenceAoUsuario.Instancia;

        if (importacao.Origem != origem)
            throw new ArgumentException("A importação é de outra origem.", nameof(importacao));

        int[]? posicoesRepetidas = lancamentos
            .Select((lancamento, indice) => (lancamento.Identidade, Posicao: indice + 1))
            .Where(l => l.Identidade.EhUnica)
            .GroupBy(l => l.Identidade, l => l.Posicao)
            .FirstOrDefault(g => g.Count() > 1)
            ?.ToArray();

        if (posicoesRepetidas is not null)
            return new ErroIdentificadorRepetido(posicoesRepetidas);

        UnitResult<Erro> beneficiarios = AliasesBeneficiarioPendentes.Conferir(idUsuario, beneficiariosDoUsuario, lancamentos, origem.Tipo);
        if (beneficiarios.IsFailure)
            return beneficiarios.Error;

        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(banco, origem, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => Transacao.Criar(idUsuario, importacao.Id, origem, lancamento, chave))
            .ToList();
    }
}
