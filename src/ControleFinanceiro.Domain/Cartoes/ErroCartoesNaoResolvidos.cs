using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// A fatura tem um final sem cartão do usuário no banco dela. Não leva os finais: a tela refaz a prévia para saber
/// quais são.
/// </summary>
public sealed class ErroCartoesNaoResolvidos : Erro
{
    public static readonly ErroCartoesNaoResolvidos Instancia = new();

    internal ErroCartoesNaoResolvidos()
        : base(
            "Cartoes.NaoResolvidos",
            "O arquivo tem finais de cartão que o usuário ainda não cadastrou; refaça a prévia e cadastre-os antes de importar.")
    {
    }
}
