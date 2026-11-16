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
                .WithTools<ListarCartoesTool>()
                .WithTools<ListarCandidatosPagamentoFaturaTool>()
                .ComFerramentasPrevia()
                .ComFerramentasConfirmacao();

            return services
                .AddTransient<IUsuarioAtual, UsuarioAtualConfigurado>()
                .AddTransient<LeituraParaModeloService>()
                .AddTransient<VinculoParaModeloService>()
                .AddTransient<PagamentoFaturaParaModeloService>()
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

    extension(IMcpServerBuilder servidor)
    {
        /// <summary>As ferramentas que mostram, sem gravar, o que a confirmação gravaria.</summary>
        private IMcpServerBuilder ComFerramentasPrevia()
        {
            return servidor
                .WithTools<PreverVinculoTool>()
                .WithTools<PreverPagamentoFaturaTool>()
                .WithTools<PreverImportacaoC6ContaCsvTool>()
                .WithTools<PreverImportacaoMercadoPagoContaCsvTool>()
                .WithTools<PreverImportacaoC6CartaoCsvTool>()
                .WithTools<PreverImportacaoMercadoPagoCartaoCsvTool>();
        }

        /// <summary>As ferramentas que gravam, depois que o usuário aprova o resumo da prévia no cliente.</summary>
        private IMcpServerBuilder ComFerramentasConfirmacao()
        {
            return servidor
                .WithTools<ConfirmarVinculoTool>()
                .WithTools<ConfirmarPagamentoFaturaTool>()
                .WithTools<ConfirmarImportacaoC6ContaCsvTool>()
                .WithTools<ConfirmarImportacaoMercadoPagoContaCsvTool>()
                .WithTools<ConfirmarImportacaoC6CartaoCsvTool>()
                .WithTools<ConfirmarImportacaoMercadoPagoCartaoCsvTool>();
        }
    }
}
