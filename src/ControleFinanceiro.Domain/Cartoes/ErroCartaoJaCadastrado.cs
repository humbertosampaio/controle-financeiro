using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>O usuário já tem um cartão do mesmo banco com o mesmo final.</summary>
public sealed class ErroCartaoJaCadastrado : Erro
{
    internal ErroCartaoJaCadastrado(Banco banco, SufixoCartao sufixo)
        : base("Cartoes.JaCadastrado", $"O usuário já tem um cartão {banco.Name} com final {sufixo}.")
    {
    }
}
