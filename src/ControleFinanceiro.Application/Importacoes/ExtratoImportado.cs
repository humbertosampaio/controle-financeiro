using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O relato da importação de um extrato, para quem a pediu montar a própria resposta. Nada do conteúdo dos lançamentos
/// entra nele.
/// </summary>
/// <param name="ContaNova">Verdadeiro quando a importação criou a conta.</param>
/// <param name="PeriodoNaoImportado">Os dias entre a importação anterior da conta e esta que nenhum arquivo cobriu.</param>
/// <param name="SomaPeriodo">Falha, com o aviso em texto, quando a soma das transações gravadas no período não fecha com a do arquivo.</param>
public sealed record ExtratoImportado(
    IdImportacao IdImportacao,
    IdConta IdConta,
    bool ContaNova,
    Banco Banco,
    TipoConta TipoConta,
    PeriodoArquivo Periodo,
    int Lidas,
    int Novas,
    int JaExistentes,
    Maybe<PeriodoArquivo> PeriodoNaoImportado,
    Result SomaPeriodo);
