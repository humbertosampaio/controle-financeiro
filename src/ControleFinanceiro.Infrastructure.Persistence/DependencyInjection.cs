using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;
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
                .AddUsuarios();
        }

        private IServiceCollection AddUsuarios()
        {
            return services.AddTransient<IUsuarioRepository, UsuarioRepository>();
        }
    }
}
