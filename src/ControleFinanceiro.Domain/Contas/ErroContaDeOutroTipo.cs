using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Contas;

/// <summary>
/// O usuário já tem uma conta do mesmo banco com o mesmo final, mas de outro tipo.
/// </summary>
public sealed class ErroContaDeOutroTipo : Erro
{
    public TipoConta TipoExistente { get; }

    internal ErroContaDeOutroTipo(Banco banco, SufixoConta sufixo, TipoConta tipoExistente)
        : base("Contas.DeOutroTipo", $"O usuário já tem uma conta {banco.Name} com final {sufixo}, mas do tipo {tipoExistente.Name}.")
    {
        TipoExistente = tipoExistente;
    }
}
