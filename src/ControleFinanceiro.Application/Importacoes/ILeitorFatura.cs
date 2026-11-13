using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de leitura de um arquivo de fatura de cartão. Cada layout é um adaptador, escolhido pelo
/// <see cref="Layout"/> que o usuário informa ao importar.
/// </summary>
public interface ILeitorFatura
{
    LayoutArquivoImportacao Layout { get; }

    /// <summary>
    /// Lê o arquivo inteiro. Falha quando ele não está no layout do <see cref="Layout"/> ou quando não traz
    /// lançamentos (<see cref="Fatura.Criar"/>); a mensagem cita no máximo o número da linha, e nunca o conteúdo do
    /// arquivo.
    /// </summary>
    Task<Result<Fatura, FalhaLeituraArquivo>> LerAsync(Stream arquivo, CancellationToken cancellationToken);
}
