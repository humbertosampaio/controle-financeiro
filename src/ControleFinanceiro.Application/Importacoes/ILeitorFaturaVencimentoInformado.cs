using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de leitura de uma fatura de cartão cujo arquivo não traz o vencimento: o usuário informa a data ao importar.
/// Cada layout é um adaptador, escolhido pelo <see cref="Layout"/> da rota.
/// </summary>
public interface ILeitorFaturaVencimentoInformado
{
    LayoutArquivoImportacao Layout { get; }

    /// <summary>
    /// Lê o arquivo inteiro. Falha quando ele não está no layout do <see cref="Layout"/> ou quando não traz
    /// lançamentos (<see cref="Fatura.Criar"/>); a mensagem cita no máximo o número da linha, e nunca o conteúdo do
    /// arquivo.
    /// </summary>
    /// <param name="vencimento">O vencimento da fatura, como o usuário informou.</param>
    Task<Result<Fatura, FalhaLeituraArquivo>> LerAsync(Stream arquivo, VencimentoFatura vencimento, CancellationToken cancellationToken);
}
