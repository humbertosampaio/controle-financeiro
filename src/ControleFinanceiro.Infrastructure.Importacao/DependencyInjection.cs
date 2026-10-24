using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure.Importacao;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registra um <see cref="ILeitorExtrato"/> ou um <see cref="ILeitorFatura"/> por layout; a importação escolhe
        /// pelo layout.
        /// </summary>
        public IServiceCollection AddImportacao()
        {
            return services
                .AddTransient<ILeitorExtrato, LeitorExtratoContaC6>()
                .AddTransient<ILeitorFatura, LeitorFaturaCartaoC6>();
        }
    }
}
