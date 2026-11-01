using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
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
/// beneficiário do usuário, e toda categoria do banco, de uma categoria dele.
/// </summary>
public static class LoteTransacoesFactory
{
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <param name="importacao">A importação do arquivo, da mesma conta.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outra origem, um lançamento é de outro tipo de origem, ou uma das listas traz um beneficiário
    /// ou uma categoria de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        ContaBancaria conta,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        return Criar(
            idUsuario,
            beneficiariosDoUsuario,
            categoriasDoUsuario,
            conta.IdUsuario,
            conta.Banco,
            OrigemTransacao.DaConta(conta.Id),
            importacao,
            lancamentos);
    }

    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <param name="importacao">A importação do arquivo, do mesmo cartão.</param>
    /// <exception cref="ArgumentException">
    /// Quando a importação é de outra origem, um lançamento é de outro tipo de origem, ou uma das listas traz um beneficiário
    /// ou uma categoria de outro usuário.
    /// </exception>
    public static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        Cartao cartao,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        return Criar(
            idUsuario,
            beneficiariosDoUsuario,
            categoriasDoUsuario,
            cartao.IdUsuario,
            cartao.Banco,
            OrigemTransacao.DoCartao(cartao.Id),
            importacao,
            lancamentos);
    }

    /// <summary>
    /// Os problemas do próprio arquivo, como o identificador repetido, aparecem antes dos beneficiários não resolvidos, e
    /// estes, antes das categorias do banco não resolvidas.
    /// </summary>
    private static Result<IReadOnlyList<Transacao>, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
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

        if (lancamentos.Any(l => l.TipoOrigem != origem.Tipo))
            throw new ArgumentException("A lista traz um lançamento de outro tipo de origem.", nameof(lancamentos));

        return ConferirIdentificadores(lancamentos)
            .Bind(() => AliasesBeneficiarioPendentes.Conferir(idUsuario, beneficiariosDoUsuario, lancamentos))
            .Bind(() => AliasesCategoriaPendentes.Conferir(idUsuario, categoriasDoUsuario, lancamentos))
            .Map(() => CriarTransacoes(idUsuario, banco, origem, importacao, lancamentos));
    }

    /// <summary>Falha com as posições do primeiro identificador único que aparece mais de uma vez no arquivo.</summary>
    private static UnitResult<Erro> ConferirIdentificadores(IReadOnlyList<Lancamento> lancamentos)
    {
        int[]? posicoesRepetidas = lancamentos
            .Select((lancamento, indice) => (lancamento.Identidade, Posicao: indice + 1))
            .Where(l => l.Identidade.EhUnica)
            .GroupBy(l => l.Identidade, l => l.Posicao)
            .FirstOrDefault(g => g.Count() > 1)?
            .ToArray();

        return posicoesRepetidas is null
            ? UnitResult.Success<Erro>()
            : new ErroIdentificadorRepetido(posicoesRepetidas);
    }

    private static IReadOnlyList<Transacao> CriarTransacoes(
        IdUsuario idUsuario,
        Banco banco,
        OrigemTransacao origem,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        IReadOnlyList<ChaveDeduplicacao> chaves = ChaveDeduplicacao.CalcularLote(banco, origem, lancamentos.Select(l => l.Identidade));

        return lancamentos
            .Zip(chaves, (lancamento, chave) => Transacao.Criar(idUsuario, importacao.Id, origem, lancamento, chave))
            .ToArray();
    }
}
