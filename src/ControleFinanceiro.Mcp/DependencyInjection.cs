using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Despesas;
using ControleFinanceiro.Application.IA.Escritas;
using ControleFinanceiro.Application.IA.Leituras;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Mcp.Autenticacao;
using ControleFinanceiro.Mcp.Ferramentas;
using Microsoft.Extensions.DependencyInjection;

namespace ControleFinanceiro.Mcp;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        /// <summary>Registra o servidor MCP, as ferramentas, uma a uma, e os serviços que elas usam.</summary>
        public IServiceCollection AddMcp()
        {
            services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithTools<VerificarServidorTool>()
                .WithTools<ListarTransacoesTool>()
                .WithTools<ListarDespesasFixasTool>()
                .WithTools<BalancoMensalTool>()
                .WithTools<PreverVinculoTool>()
                .WithTools<ConfirmarVinculoTool>()
                .WithTools<ListarCartoesTool>()
                .WithTools<ListarCandidatosPagamentoFaturaTool>()
                .WithTools<PreverPagamentoFaturaTool>()
                .WithTools<ConfirmarPagamentoFaturaTool>()
                .WithTools<PreverImportacaoTool>()
                .WithTools<ConfirmarImportacaoTool>()
                .WithTools<PreverImportacaoC6ContaCsvTool>()
                .WithTools<ConfirmarImportacaoC6ContaCsvTool>()
                .WithTools<PreverImportacaoMercadoPagoContaCsvTool>()
                .WithTools<ConfirmarImportacaoMercadoPagoContaCsvTool>()
                .WithTools<PreverImportacaoC6CartaoCsvTool>()
                .WithTools<ConfirmarImportacaoC6CartaoCsvTool>()
                .WithTools<PreverImportacaoMercadoPagoCartaoCsvTool>()
                .WithTools<ConfirmarImportacaoMercadoPagoCartaoCsvTool>();

            return services
                .AddTransient<IUsuarioAtual, UsuarioAtualConfigurado>()
                .AddTransient<LeituraParaModeloService>()
                .AddTransient<VinculoParaModeloService>()
                .AddTransient<PagamentoFaturaParaModeloService>()
                .AddTransient<ImportacaoParaModeloService>()
                .AddTransient<ImportacaoExtratoParaModeloService>()
                .AddTransient<ImportacaoFaturaParaModeloService>()
                .AddTransient<ConfirmacaoImportacaoParaModeloService>()
                .AddTransient<LeituraExtratoAppService>()
                .AddTransient<LeituraFaturaAppService>()
                .AddTransient<ImportacaoExtratoAppService>()
                .AddTransient<ImportacaoFaturaAppService>()
                .AddTransient<PendenciasImportacaoAppService>()
                .AddTransient<CandidatosPagamentoFaturaAppService>()
                .AddSingleton(TimeProvider.System);
        }
    }
}
