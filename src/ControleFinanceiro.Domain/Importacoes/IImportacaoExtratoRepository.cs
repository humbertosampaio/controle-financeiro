using ControleFinanceiro.Domain.Contas;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>Enxerga só as importações de extrato do usuário atual.</summary>
public interface IImportacaoExtratoRepository
{
    Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken cancellationToken);

    /// <summary>
    /// A importação da conta cujo período termina mais tarde, para apontar o período que nenhum arquivo importou.
    /// </summary>
    Task<Maybe<ImportacaoExtrato>> ObterMaisRecenteAsync(IdConta idConta, CancellationToken cancellationToken);
}
