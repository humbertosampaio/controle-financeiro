using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Infrastructure.Importacao.Arquivos;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using ControleFinanceiro.Infrastructure.Importacao.MercadoPago;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Infrastructure.Importacao;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registra um <see cref="ILeitorExtrato"/>, um <see cref="ILeitorExtratoContaInformada"/> ou um
        /// <see cref="ILeitorFatura"/> por layout; a importação escolhe pelo layout.
        /// </summary>
        public IServiceCollection AddInfrastructureImportacao()
        {
            return services
                .AddTransient<ILeitorExtrato, LeitorExtratoContaC6>()
                .AddTransient<ILeitorExtratoContaInformada, LeitorExtratoContaMercadoPago>()
                .AddTransient<ILeitorFatura, LeitorFaturaCartaoC6>()
                .AddTransient<ILeitorFatura, LeitorFaturaCartaoMercadoPago>();
        }

        /// <summary>Registra a <see cref="IOrigemArquivoImportacao"/> na pasta <paramref name="raiz"/>.</summary>
        public IServiceCollection AddOrigemArquivoImportacao(string raiz)
        {
            return services.AddSingleton<IOrigemArquivoImportacao>(new OrigemArquivoImportacaoLocal(raiz));
        }
    }
}
