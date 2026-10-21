using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de leitura de um arquivo de extrato ou fatura. Cada layout é um adaptador, escolhido pelo
/// <see cref="Formato"/> que o usuário informa ao importar.
/// </summary>
public interface ILeitorExtrato
{
    FormatoExtrato Formato { get; }

    /// <summary>
    /// Lê o arquivo inteiro. Falha quando ele não está no layout do <see cref="Formato"/>; a mensagem cita
    /// no máximo o número da linha, e nunca o conteúdo do arquivo.
    /// </summary>
    Task<Result<ExtratoLido>> LerAsync(Stream arquivo, CancellationToken cancellationToken);
}
