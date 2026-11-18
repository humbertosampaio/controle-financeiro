using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence.Beneficiarios;
using ControleFinanceiro.Infrastructure.Persistence.Cartoes;
using ControleFinanceiro.Infrastructure.Persistence.Categorias;
using ControleFinanceiro.Infrastructure.Persistence.Contas;
using ControleFinanceiro.Infrastructure.Persistence.Despesas;
using ControleFinanceiro.Infrastructure.Persistence.Importacoes;
using ControleFinanceiro.Infrastructure.Persistence.Outbox;
using ControleFinanceiro.Infrastructure.Persistence.Transacoes;
using ControleFinanceiro.Infrastructure.Persistence.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ControleFinanceiro.Infrastructure.Persistence;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructurePersistence(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("controlefinanceiro");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'controlefinanceiro' não encontrada.");

            // Os agregados vêm inteiros (AutoInclude); split query evita o produto cartesiano entre as coleções.
            // Sem pooling: o DbContext recebe pelo construtor o IUsuarioAtual, que cada host registra.
            services.AddDbContext<ControleFinanceiroDbContext>(options =>
                options.UseNpgsql(
                    connectionString,
                    npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

            // O host pode registrar outro relógio antes; o outbox só precisa de um.
            services.TryAddSingleton(TimeProvider.System);

            return services
                .AddTransient<IUnitOfWork, EntityFrameworkUnitOfWork>()
                .AddTransient<MensagemOutboxRepository>()
                .AddTransient<IUsuarioRepository, UsuarioRepository>()
                .AddTransient<ICartaoRepository, CartaoRepository>()
                .AddTransient<IContaRepository, ContaRepository>()
                .AddTransient<ITransacaoRepository, TransacaoRepository>()
                .AddTransient<ITransacaoContaRepository, TransacaoContaRepository>()
                .AddTransient<ITransacaoCartaoRepository, TransacaoCartaoRepository>()
                .AddTransient<IImportacaoExtratoRepository, ImportacaoExtratoRepository>()
                .AddTransient<IImportacaoFaturaRepository, ImportacaoFaturaRepository>()
                .AddTransient<IBeneficiarioRepository, BeneficiarioRepository>()
                .AddTransient<ICategoriaDespesaRepository, CategoriaDespesaRepository>()
                .AddTransient<IDespesaRecorrenteRepository, DespesaRecorrenteRepository>()
                .AddTransient<IDespesaRepository, DespesaRepository>();
        }
    }
}
