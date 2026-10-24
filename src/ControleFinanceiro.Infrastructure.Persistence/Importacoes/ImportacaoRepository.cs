using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoRepository : IImportacaoRepository
{
    private readonly DbSet<ImportacaoArquivo> _dbSet;

    public ImportacaoRepository(ControleFinanceiroDbContext dbContext)
    {
        _dbSet = dbContext.Importacoes;
    }

    public async Task AdicionarAsync(ImportacaoArquivo importacao, CancellationToken cancellationToken) =>
        await _dbSet.AddAsync(importacao, cancellationToken);

    public async Task<Maybe<ImportacaoArquivo>> ObterMaisRecenteAsync(OrigemTransacao origem, CancellationToken cancellationToken)
    {
        return await DaOrigem(origem)
            .OrderByDescending(i => i.Periodo.Fim)
            .ThenByDescending(i => i.ImportadaEm)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Um filtro por coluna, e não a comparação das duas, para a consulta usar o índice da conta.
    private IQueryable<ImportacaoArquivo> DaOrigem(OrigemTransacao origem)
    {
        return origem.IdConta is IdConta idConta
            ? _dbSet.Where(i => i.Origem.IdConta == idConta)
            : _dbSet.Where(i => i.Origem.IdCartao == origem.IdCartao);
    }
}
