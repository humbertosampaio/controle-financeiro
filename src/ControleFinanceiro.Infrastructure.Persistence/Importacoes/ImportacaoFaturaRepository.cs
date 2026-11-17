using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoFaturaRepository : IImportacaoFaturaRepository
{
    private readonly DbSet<ImportacaoArquivo> _dbSet;

    public ImportacaoFaturaRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Importacoes;
    }

    public async Task AdicionarAsync(ImportacaoFatura importacao, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(importacao, cancellationToken);

    public async Task<IReadOnlyList<ImportacaoFatura>> ListarEmAsync(MesReferencia mesVencimento, CancellationToken cancellationToken)
    {
        return await _dbSet
            .OfType<ImportacaoFatura>()
            .Where(i => i.Vencimento >= mesVencimento.Inicio && i.Vencimento <= mesVencimento.Fim)
            .ToListAsync(cancellationToken);
    }
}
