using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// Escolhe o leitor do extrato pelo layout, em qualquer rota: o layout cujo arquivo não traz a conta é lido com o
/// sufixo que o usuário informou.
/// </summary>
public sealed class LeituraExtratoService
{
    private readonly IEnumerable<ILeitorExtrato> _leitores;
    private readonly IEnumerable<ILeitorExtratoContaInformada> _leitoresContaInformada;

    public LeituraExtratoService(IEnumerable<ILeitorExtrato> leitores, IEnumerable<ILeitorExtratoContaInformada> leitoresContaInformada)
    {
        _leitores = leitores;
        _leitoresContaInformada = leitoresContaInformada;
    }

    /// <param name="layout">Um layout de extrato.</param>
    /// <param name="sufixoConta">Presente quando o layout é de conta informada; nos outros, não é lido.</param>
    /// <exception cref="InvalidOperationException">Quando o layout é de conta informada e o sufixo não veio.</exception>
    public Task<Result<Extrato, FalhaLeituraArquivo>> LerAsync(
        LayoutArquivoImportacao layout,
        Stream arquivo,
        Maybe<SufixoConta> sufixoConta,
        CancellationToken ct)
    {
        return LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout)
            ? _leitoresContaInformada
                .Single(l => l.Layout == layout)
                .LerAsync(arquivo, sufixoConta.GetValueOrThrow($"O layout {layout.Value} pede o sufixo da conta."), ct)
            : _leitores.Single(l => l.Layout == layout).LerAsync(arquivo, ct);
    }
}
