using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.IA.Saida;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Confirmacao;

/// <summary>
/// Pergunta ao usuário pela elicitation do MCP, que o cliente mostra a ele sem passar pelo modelo. Usa o
/// <see cref="McpServer"/> da requisição, e não o do DI, porque as capacidades do cliente valem só nele. Qualquer coisa
/// que não seja o aceite com a confirmação marcada não confirma.
/// </summary>
internal sealed partial class ConfirmacaoPorElicitacao : IConfirmacaoHumana
{
    private readonly McpServer _servidor;
    private readonly ILogger<ConfirmacaoPorElicitacao> _logger;

    public ConfirmacaoPorElicitacao(McpServer servidor, ILogger<ConfirmacaoPorElicitacao> logger)
    {
        _servidor = servidor;
        _logger = logger;
    }

    public async Task<RespostaConfirmacao> PedirAsync(VinculoParaModelo previa, CancellationToken cancellationToken)
    {
        if (!AceitaFormulario(_servidor.ClientCapabilities?.Elicitation))
            return RespostaConfirmacao.Indisponivel;

        try
        {
            ElicitResult<FormularioConfirmacao> resultado = await _servidor.ElicitAsync<FormularioConfirmacao>(
                previa.Mensagem!,
                options: null,
                cancellationToken);

            return resultado.IsAccepted && resultado.Content?.Confirmar == true
                ? RespostaConfirmacao.Confirmada
                : RespostaConfirmacao.Recusada;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A exceção do SDK não pode ser nomeada aqui (SaidaModeloArquiteturaTests): falhar ao perguntar é não confirmar.
            // Ela não volta ao modelo, então fica no log, que vai para o stderr do processo.
            LogFalhaAoPerguntar(ex);
            return RespostaConfirmacao.Indisponivel;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "A confirmação do usuário não pôde ser pedida; nada foi gravado.")]
    private partial void LogFalhaAoPerguntar(Exception ex);

    /// <summary>A capacidade sem modo nenhum é a de antes dos modos, que só conhecia o formulário.</summary>
    private static bool AceitaFormulario(ElicitationCapability? elicitacao) =>
        elicitacao is not null && (elicitacao.Form is not null || elicitacao.Url is null);
}
