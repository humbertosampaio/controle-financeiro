namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>
/// O resumo da importação e os avisos, que não impedem a gravação. Nada do conteúdo dos lançamentos volta na
/// resposta.
/// </summary>
/// <param name="PeriodoNaoImportado">Os dias entre a importação anterior da conta e esta que nenhum arquivo cobriu.</param>
/// <param name="AvisoSomaPeriodo">Presente quando a soma das transações gravadas no período não fecha com a do arquivo.</param>
public sealed record class ImportarExtratoResponse(
    Guid IdImportacao,
    Guid IdConta,
    int Lidas,
    int Novas,
    int JaExistentes,
    ImportarExtratoResponse.Periodo? PeriodoNaoImportado,
    string? AvisoSomaPeriodo)
{
    public sealed record class Periodo(DateOnly Inicio, DateOnly Fim);
}
