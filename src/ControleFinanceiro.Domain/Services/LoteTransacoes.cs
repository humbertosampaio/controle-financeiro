using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// As conferências do lote que valem para o extrato e para a fatura, comuns à <see cref="LoteTransacoesContaFactory"/> e
/// à <see cref="LoteTransacoesCartaoFactory"/>.
/// </summary>
internal static class LoteTransacoes
{
    /// <summary>
    /// Falha quando a conta ou o cartão é de outro usuário. Depois, os problemas do próprio arquivo, como o identificador
    /// repetido, aparecem antes dos beneficiários não resolvidos, e estes, antes das categorias do banco não resolvidas.
    /// </summary>
    /// <param name="donoOrigem">O dono da conta ou do cartão do arquivo.</param>
    /// <exception cref="ArgumentException">Quando uma das listas traz um beneficiário ou uma categoria de outro usuário.</exception>
    internal static UnitResult<Erro> Conferir(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IdUsuario donoOrigem,
        IReadOnlyList<Lancamento> lancamentos)
    {
        if (donoOrigem != idUsuario)
            return ErroOrigemNaoPertenceAoUsuario.Instancia;

        return ConferirIdentificadores(lancamentos)
            .Bind(() => AliasesBeneficiarioPendentes.Conferir(idUsuario, beneficiariosDoUsuario, lancamentos))
            .Bind(() => AliasesCategoriaPendentes.Conferir(idUsuario, categoriasDoUsuario, lancamentos));
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
}
