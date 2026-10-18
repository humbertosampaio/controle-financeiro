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

            services.AddDbContext<ControleFinanceiroDbContext>(options =>
                options.UseNpgsql(connectionString));

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
