using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Beneficiarios;

/// <summary>
/// O arquivo tem uma saída cujo texto de beneficiário ainda não é alias de nenhum beneficiário do usuário. Não leva os
/// textos: a tela refaz a prévia para saber quais são.
/// </summary>
public sealed class ErroBeneficiariosNaoResolvidos : Erro
{
    public static readonly ErroBeneficiariosNaoResolvidos Instancia = new();

    internal ErroBeneficiariosNaoResolvidos()
        : base(
            "Beneficiarios.NaoResolvidos",
            "O arquivo tem beneficiários que ainda não são alias de nenhum beneficiário do usuário; refaça a prévia e resolva-os antes de importar.")
    {
    }
}
