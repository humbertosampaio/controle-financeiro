using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence.Conventions;
using ControleFinanceiro.Infrastructure.Persistence.ValueConverters;
using ControleFinanceiro.Infrastructure.Persistence.ValueConverters.ValueObjects;
using Microsoft.EntityFrameworkCore;
using SmartEnum.EFCore;
using System.Reflection;

namespace ControleFinanceiro.Infrastructure.Persistence;

public sealed class ControleFinanceiroDbContext : DbContext
{
    private readonly Assembly _assembly = typeof(ControleFinanceiroDbContext).Assembly;

    public ControleFinanceiroDbContext(DbContextOptions<ControleFinanceiroDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cartao> Cartoes => Set<Cartao>();
    public DbSet<ContaBancaria> Contas => Set<ContaBancaria>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<ImportacaoArquivo> Importacoes => Set<ImportacaoArquivo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DespesaRecorrente>();
        modelBuilder.ApplyConfigurationsFromAssembly(_assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TextoObrigatorio>()
            .HaveConversion<TextoObrigatorioConverter>();

        configurationBuilder.ConfigurarDinheiro();

        configurationBuilder.RegisterAllInVogenEfCoreConverters();

        configurationBuilder.ConfigureSmartEnum();

        configurationBuilder
            .Properties<Banco>()
            .HaveMaxLength(Banco.QuantidadeDigitosCodigoCompe);
    }
}
