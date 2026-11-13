using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarExtrato;

/// <summary>
/// Lê o extrato no layout da rota, com o mesmo formulário e as mesmas regras da importação, e devolve o que o usuário
/// precisa resolver antes de importá-lo. Nada é gravado, e o arquivo não fica no servidor.
/// </summary>
public sealed class PreVisualizarExtratoEndpoint : Endpoint<ImportarExtratoRequest, PreVisualizarExtratoResponse>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly LeituraExtratoService _leituraExtrato;
    private readonly PendenciasImportacaoService _pendencias;

    public PreVisualizarExtratoEndpoint(
        IUsuarioAtual usuarioAtual,
        LeituraExtratoService leituraExtrato,
        PendenciasImportacaoService pendencias)
    {
        _usuarioAtual = usuarioAtual;
        _leituraExtrato = leituraExtrato;
        _pendencias = pendencias;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes/extratos/{layout}/previa");
        AllowFileUploads();
        AllowAnonymous();

        // O mesmo limite da importação, pelo mesmo motivo: sem ele, o formulário é lido inteiro antes do validator.
        Options(rota => rota.WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = ImportarExtratoEndpoint.TamanhoMaximoFormulario }));
    }

    public override async Task HandleAsync(ImportarExtratoRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = _usuarioAtual.Id;
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(req.Layout);

        await LerAsync(layout, req, ct)
            .Map(extrato => PreVisualizarAsync(idUsuario, extrato, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    /// <param name="req">Já validada: traz o sufixo quando o layout pede.</param>
    private async Task<Result<Extrato, Falha>> LerAsync(LayoutArquivoImportacao layout, ImportarExtratoRequest req, CancellationToken ct)
    {
        await using Stream conteudo = req.Arquivo!.OpenReadStream();

        return await _leituraExtrato
            .LerAsync(layout, conteudo, SufixoDoLayout(layout, req), ct)
            .MapError(Falha.ArquivoInvalido);
    }

    /// <summary>O validator já garantiu o sufixo no layout de conta informada; nos outros, ele não vale.</summary>
    private static Maybe<SufixoConta> SufixoDoLayout(LayoutArquivoImportacao layout, ImportarExtratoRequest req)
    {
        return LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout)
            ? (SufixoConta)req.SufixoConta
            : Maybe<SufixoConta>.None;
    }

    private async Task<PreVisualizarExtratoResponse> PreVisualizarAsync(IdUsuario idUsuario, Extrato extrato, CancellationToken ct)
    {
        IReadOnlyList<Alias> pendentes = await _pendencias.ListarDoExtratoAsync(idUsuario, extrato, ct);

        return new PreVisualizarExtratoResponse(pendentes.Select(a => a.TextoOriginal).ToList());
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Arquivo(string mensagem):
                AddError(r => r.Arquivo, mensagem);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha.GetType().Name}");
        }
    }

    /// <summary>
    /// O que interrompe a prévia, num tipo só para a cadeia: o arquivo que o leitor recusou, inclusive pelo saldo que
    /// não fecha (mensagem em texto).
    /// </summary>
    private abstract record Falha
    {
        public static Falha ArquivoInvalido(FalhaLeituraArquivo falha) => new Arquivo(falha.Mensagem);

        public sealed record Arquivo(string Mensagem) : Falha;
    }
}
