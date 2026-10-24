using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de leitura de um arquivo de extrato de conta. Cada layout é um adaptador, escolhido pelo
/// <see cref="Layout"/> que o usuário informa ao importar.
/// </summary>
public interface ILeitorExtrato
{
    LayoutArquivoImportacao Layout { get; }

    /// <summary>
    /// Lê o arquivo inteiro. Falha quando ele não está no layout do <see cref="Layout"/> ou quando os saldos
    /// não fecham com os lançamentos (<see cref="Extrato.Criar"/>); a mensagem cita no máximo o número da
    /// linha ou as datas, e nunca o conteúdo do arquivo.
    /// </summary>
    Task<Result<Extrato>> LerAsync(Stream arquivo, CancellationToken cancellationToken);
}
