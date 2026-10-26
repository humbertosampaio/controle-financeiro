using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria o cartão do usuário garantindo um só cartão por banco e final, ou obtém os cartões dos finais de uma
/// fatura. O índice único da tabela é a rede para duas requisições ao mesmo tempo.
/// </summary>
public static class CartaoFactory
{
    /// <param name="cartoesDoUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um cartão de outro usuário.</exception>
    public static Result<Cartao, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento)
    {
        GarantirDoUsuario(idUsuario, cartoesDoUsuario);

        if (DoFinal(cartoesDoUsuario, banco, sufixo).HasValue)
            return new ErroCartaoJaCadastrado(banco, sufixo);

        return Cartao.Criar(idUsuario, banco, sufixo, diaVencimento);
    }

    /// <summary>
    /// Devolve um cartão por final distinto, na ordem em que os finais aparecem: o do usuário no mesmo banco, sem
    /// mudança, ou um novo, com o vencimento que <paramref name="vencimentos"/> dá ao final.
    /// </summary>
    /// <remarks>
    /// Só os cartões novos precisam ser adicionados ao repositório: quem chama sabe que um cartão é novo porque não
    /// está na lista que passou (<c>!cartoesDoUsuario.Contains(cartao)</c>).
    /// </remarks>
    /// <param name="cartoesDoUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um cartão de outro usuário.</exception>
    public static IReadOnlyList<Cartao> ObterOuCriar(
        IdUsuario idUsuario,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        Banco banco,
        IEnumerable<SufixoCartao> sufixos,
        VencimentosCartoesNovos vencimentos)
    {
        GarantirDoUsuario(idUsuario, cartoesDoUsuario);

        return sufixos
            .Distinct()
            .Select(sufixo => DoFinal(cartoesDoUsuario, banco, sufixo)
                .GetValueOrDefault(() => Cartao.Criar(idUsuario, banco, sufixo, vencimentos.Para(sufixo))))
            .ToList();
    }

    /// <summary>O cartão do usuário com o final no mesmo banco, o que a importação usa em vez de criar outro.</summary>
    /// <param name="cartoesDoUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz um cartão de outro usuário.</exception>
    public static Maybe<Cartao> ObterDoFinal(
        IdUsuario idUsuario,
        IReadOnlyCollection<Cartao> cartoesDoUsuario,
        Banco banco,
        SufixoCartao sufixo)
    {
        GarantirDoUsuario(idUsuario, cartoesDoUsuario);
        return DoFinal(cartoesDoUsuario, banco, sufixo);
    }

    private static Maybe<Cartao> DoFinal(IReadOnlyCollection<Cartao> cartoes, Banco banco, SufixoCartao sufixo)
    {
        return cartoes.TryFirst(c => c.Banco == banco && c.Sufixo == sufixo);
    }

    private static void GarantirDoUsuario(IdUsuario idUsuario, IReadOnlyCollection<Cartao> cartoesDoUsuario)
    {
        if (cartoesDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesDoUsuario));
    }
}
