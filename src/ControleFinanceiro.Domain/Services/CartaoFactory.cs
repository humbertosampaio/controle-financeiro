using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria o cartão do usuário garantindo um só cartão por banco e final. O índice único da tabela é a rede para
/// duas requisições ao mesmo tempo.
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
        if (cartoesDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesDoUsuario));

        if (cartoesDoUsuario.Any(c => c.Banco == banco && c.Sufixo == sufixo))
            return new ErroCartaoJaCadastrado(banco, sufixo);

        return Cartao.Criar(idUsuario, banco, sufixo, diaVencimento);
    }
}
