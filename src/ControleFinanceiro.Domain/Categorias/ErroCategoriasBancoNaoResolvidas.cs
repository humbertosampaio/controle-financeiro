using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Categorias;

/// <summary>
/// O arquivo tem um lançamento cuja categoria do banco ainda não é alias de nenhuma categoria do usuário. Não leva os
/// textos: a tela refaz a prévia para saber quais são.
/// </summary>
public sealed class ErroCategoriasBancoNaoResolvidas : Erro
{
    public static readonly ErroCategoriasBancoNaoResolvidas Instancia = new();

    internal ErroCategoriasBancoNaoResolvidas()
        : base(
            "Categorias.BancoNaoResolvidas",
            "O arquivo tem categorias do banco que ainda não são alias de nenhuma categoria do usuário; refaça a prévia e resolva-as antes de importar.")
    {
    }
}
