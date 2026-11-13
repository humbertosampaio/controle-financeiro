using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Porta de leitura de um extrato de conta cujo arquivo não traz a conta: o usuário informa o sufixo ao importar.
/// Cada layout é um adaptador, escolhido pelo <see cref="Layout"/> da rota.
/// </summary>
public interface ILeitorExtratoContaInformada
{
    LayoutArquivoImportacao Layout { get; }

    /// <summary>
    /// Lê o arquivo inteiro. Falha quando ele não está no layout do <see cref="Layout"/> ou quando os saldos não
    /// fecham com os lançamentos (<see cref="Extrato.Criar"/>); a mensagem cita no máximo o número da linha ou as
    /// datas, e nunca o conteúdo do arquivo.
    /// </summary>
    /// <param name="sufixoConta">O sufixo da conta de onde veio o arquivo, como o usuário informou.</param>
    Task<Result<Extrato, FalhaLeituraArquivo>> LerAsync(Stream arquivo, SufixoConta sufixoConta, CancellationToken cancellationToken);
}
