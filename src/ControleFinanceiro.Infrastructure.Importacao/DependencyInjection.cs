using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure.Importacao;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>Registra um <see cref="ILeitorExtrato"/> por layout; a importação escolhe pelo formato.</summary>
        public IServiceCollection AddImportacao()
        {
            return services.AddTransient<ILeitorExtrato, LeitorExtratoContaC6>();
        }
    }
}
