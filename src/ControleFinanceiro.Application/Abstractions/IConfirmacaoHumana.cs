using ControleFinanceiro.Application.IA.Saida;

namespace ControleFinanceiro.Application.Abstractions;

/// <summary>
/// Pergunta ao usuário, e não ao modelo, se uma escrita pedida pelo modelo pode ser gravada. Cada host que fala com o
/// modelo registra o seu adaptador.
/// </summary>
public interface IConfirmacaoHumana
{
    /// <param name="previa">O vínculo já aplicado em memória: o que o usuário confirma é o que vai ser gravado.</param>
    Task<RespostaConfirmacao> PedirAsync(VinculoParaModelo previa, CancellationToken cancellationToken);
}
