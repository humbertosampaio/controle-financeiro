using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence.Cartoes;
using ControleFinanceiro.Infrastructure.Persistence.Contas;
using ControleFinanceiro.Infrastructure.Persistence.Importacoes;
using ControleFinanceiro.Infrastructure.Persistence.Transacoes;
using ControleFinanceiro.Infrastructure.Persistence.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure.Persistence;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPersistence(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("controlefinanceiro");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'controlefinanceiro' não encontrada.");

            // Os agregados vêm inteiros (AutoInclude); split query evita o produto cartesiano entre as coleções.
            services.AddDbContext<ControleFinanceiroDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            return services
                .AddScoped<IUnitOfWork, EntityFrameworkUnitOfWork>()
                .AddUsuarios()
                .AddCartoes()
                .AddContas()
                .AddTransacoes()
                .AddImportacoes();
        }

        private IServiceCollection AddUsuarios()
        {
            return services.AddTransient<IUsuarioRepository, UsuarioRepository>();
        }

        private IServiceCollection AddCartoes()
        {
            return services.AddTransient<ICartaoRepository, CartaoRepository>();
        }

        private IServiceCollection AddContas()
        {
            return services.AddTransient<IContaRepository, ContaRepository>();
        }

        private IServiceCollection AddTransacoes()
        {
            return services.AddTransient<ITransacaoRepository, TransacaoRepository>();
        }

        private IServiceCollection AddImportacoes()
        {
            return services.AddTransient<IImportacaoRepository, ImportacaoRepository>();
        }
    }
}
