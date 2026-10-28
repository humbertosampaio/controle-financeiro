using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
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
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly LeituraExtratoService _leituraExtrato;

    public PreVisualizarExtratoEndpoint(
        IUsuarioAtual usuarioAtual,
        IBeneficiarioRepository beneficiarioRepository,
        LeituraExtratoService leituraExtrato)
    {
        _usuarioAtual = usuarioAtual;
        _beneficiarioRepository = beneficiarioRepository;
        _leituraExtrato = leituraExtrato;
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
            .LerAsync(layout, conteudo, req.SufixoConta, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    private async Task<PreVisualizarExtratoResponse> PreVisualizarAsync(IdUsuario idUsuario, Extrato extrato, CancellationToken ct)
    {
        IReadOnlyList<Beneficiario> beneficiarios = await _beneficiarioRepository.ListarAsync(ct);
        IReadOnlyList<Alias> pendentes = AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiarios, extrato.Lancamentos, TipoOrigem.Conta);

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
        public static Falha ArquivoInvalido(string mensagem) => new Arquivo(mensagem);

        public sealed record Arquivo(string Mensagem) : Falha;
    }
}
