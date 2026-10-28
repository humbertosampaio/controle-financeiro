using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
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
using Microsoft.EntityFrameworkCore.Metadata;
using SmartEnum.EFCore;
using System.Linq.Expressions;
using System.Reflection;

namespace ControleFinanceiro.Infrastructure.Persistence;

public sealed class ControleFinanceiroDbContext : DbContext
{
    /// <summary>Nome do filtro global que restringe as consultas ao usuário atual.</summary>
    public const string FiltroUsuario = "Usuario";

    private readonly Assembly _assembly = typeof(ControleFinanceiroDbContext).Assembly;
    private readonly IUsuarioAtual _usuarioAtual;

    public ControleFinanceiroDbContext(DbContextOptions<ControleFinanceiroDbContext> options, IUsuarioAtual usuarioAtual)
        : base(options)
    {
        _usuarioAtual = usuarioAtual;
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cartao> Cartoes => Set<Cartao>();
    public DbSet<ContaBancaria> Contas => Set<ContaBancaria>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<ImportacaoArquivo> Importacoes => Set<ImportacaoArquivo>();
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<CategoriaDespesa> Categorias => Set<CategoriaDespesa>();
    public DbSet<DespesaRecorrente> DespesasRecorrentes => Set<DespesaRecorrente>();

    /// <summary>Lido pelo filtro a cada consulta: o EF troca o <c>this</c> da expressão pela instância corrente.</summary>
    private IdUsuario IdUsuarioAtual => _usuarioAtual.Id;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(_assembly);
        FiltrarPeloUsuarioAtual(modelBuilder);
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

    /// <summary>
    /// Restringe ao usuário atual toda consulta de agregado que implementa <see cref="IPertenceAoUsuario"/>.
    /// O filtro vai só na raiz da hierarquia, como o EF exige, e vale para os tipos derivados.
    /// </summary>
    private void FiltrarPeloUsuarioAtual(ModelBuilder modelBuilder)
    {
        IEnumerable<IMutableEntityType> doUsuario = modelBuilder.Model.GetEntityTypes()
            .Where(tipo => typeof(IPertenceAoUsuario).IsAssignableFrom(tipo.ClrType))
            .Where(tipo => tipo.BaseType is null && !tipo.IsOwned());

        foreach (IMutableEntityType tipo in doUsuario)
        {
            ParameterExpression entidade = Expression.Parameter(tipo.ClrType, "e");
            BinaryExpression doUsuarioAtual = Expression.Equal(
                Expression.Property(entidade, nameof(IPertenceAoUsuario.IdUsuario)),
                Expression.Property(Expression.Constant(this), nameof(IdUsuarioAtual)));

            modelBuilder.Entity(tipo.ClrType).HasQueryFilter(FiltroUsuario, Expression.Lambda(doUsuarioAtual, entidade));
        }
    }
}
