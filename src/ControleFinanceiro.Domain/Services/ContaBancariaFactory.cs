using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Acha a conta de onde veio um extrato entre as do usuário, casada por banco e final, ou cria uma. O índice
/// único da tabela é a rede para duas importações ao mesmo tempo.
/// </summary>
/// <remarks>
/// Só a conta nova precisa ser adicionada ao repositório: quem chama sabe que ela é nova porque não está na
/// lista que passou (<c>!contasDoUsuario.Contains(conta)</c>).
/// </remarks>
public static class ContaBancariaFactory
{
    /// <param name="contasDoUsuario">Todas as contas do usuário, e só as dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz uma conta de outro usuário.</exception>
    public static Result<ContaBancaria, Erro> ObterOuCriar(
        IdUsuario idUsuario,
        IReadOnlyCollection<ContaBancaria> contasDoUsuario,
        Banco banco,
        TipoConta tipo,
        SufixoConta sufixo)
    {
        if (contasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma conta de outro usuário.", nameof(contasDoUsuario));

        ContaBancaria? existente = contasDoUsuario.FirstOrDefault(c => c.Banco == banco && c.Sufixo == sufixo);
        if (existente is null)
            return ContaBancaria.Criar(idUsuario, banco, tipo, sufixo);

        if (existente.Tipo != tipo)
            return new ErroContaDeOutroTipo(banco, sufixo, existente.Tipo);

        return existente;
    }
}
