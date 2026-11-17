using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoExtratoRepository : IImportacaoExtratoRepository
{
    private readonly DbSet<ImportacaoArquivo> _dbSet;

    public ImportacaoExtratoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Importacoes;
    }

    public async Task AdicionarAsync(ImportacaoExtrato importacao, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(importacao, cancellationToken);

    public async Task<Maybe<ImportacaoExtrato>> ObterMaisRecenteAsync(IdConta idConta, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<ImportacaoExtrato>()
            .Where(i => i.IdConta == idConta)
            .OrderByDescending(i => i.Periodo.Fim)
            .ThenByDescending(i => i.ImportadaEm)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
