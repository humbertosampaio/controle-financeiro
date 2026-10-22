using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
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

        configurationBuilder
            .Properties<ReaisPositivo>()
            .HaveConversion<ReaisPositivoConverter>();

        configurationBuilder
            .Properties<Reais>()
            .HaveConversion<ReaisConverter>();

        configurationBuilder
            .Properties<DiaVencimentoCartao>()
            .HaveConversion<DiaVencimentoCartaoConverter>();

        configurationBuilder
            .Properties<SufixoCartao>()
            .HaveConversion<SufixoCartaoConverter>()
            .HaveMaxLength(SufixoCartao.QuantidadeDigitos);

        configurationBuilder
            .Properties<DiaCompraRecorrente>()
            .HaveConversion<DiaCompraRecorrenteConverter>();

        configurationBuilder.RegisterAllInVogenEfCoreConverters();

        configurationBuilder.ConfigureSmartEnum();

        configurationBuilder
            .Properties<Banco>()
            .HaveMaxLength(Banco.QuantidadeDigitosCodigoCompe);
    }
}
