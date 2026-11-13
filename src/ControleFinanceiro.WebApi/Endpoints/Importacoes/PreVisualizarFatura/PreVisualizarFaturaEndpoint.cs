using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;

/// <summary>
/// Lê a fatura no layout da rota e devolve o que o usuário precisa preencher ou resolver antes de importá-la: os finais
/// de cartão, marcando os já cadastrados, os beneficiários novos e as categorias do banco sem alias. Nada é gravado, e
/// o arquivo não fica no servidor.
/// </summary>
public sealed class PreVisualizarFaturaEndpoint : Endpoint<PreVisualizarFaturaRequest, PreVisualizarFaturaResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para os cabeçalhos do
    /// multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = PreVisualizarFaturaValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioAtual _usuarioAtual;
    private readonly LeituraFaturaService _leituraFatura;
    private readonly PendenciasImportacaoService _pendencias;

    public PreVisualizarFaturaEndpoint(
        IUsuarioAtual usuarioAtual,
        LeituraFaturaService leituraFatura,
        PendenciasImportacaoService pendencias)
    {
        _usuarioAtual = usuarioAtual;
        _leituraFatura = leituraFatura;
        _pendencias = pendencias;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes/faturas/{layout}/previa");
        AllowFileUploads();
        AllowAnonymous();

        // Sem o limite na rota, o formulário é lido inteiro, e acima de 64 KB guardado em disco, antes de o validator
        // recusar o arquivo. Acima do limite, a leitura para e a resposta é um 400 geral, sem chegar ao validator.
        Options(rota => rota.WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = TamanhoMaximoFormulario }));
    }

    public override async Task HandleAsync(PreVisualizarFaturaRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = _usuarioAtual.Id;
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(req.Layout);

        await LerAsync(layout, req.Arquivo!, ct)
            .Map(fatura => PreVisualizarAsync(idUsuario, fatura, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    private async Task<Result<Fatura, Falha>> LerAsync(LayoutArquivoImportacao layout, IFormFile arquivo, CancellationToken ct)
    {
        await using Stream conteudo = arquivo.OpenReadStream();

        return await _leituraFatura
            .LerAsync(layout, conteudo, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    private async Task<PreVisualizarFaturaResponse> PreVisualizarAsync(IdUsuario idUsuario, Fatura fatura, CancellationToken ct)
    {
        PendenciasFatura pendencias = await _pendencias.ListarDaFaturaAsync(idUsuario, fatura, ct);

        return new PreVisualizarFaturaResponse(
            pendencias.Finais.Select(LerFinal).ToArray(),
            pendencias.Beneficiarios.Select(a => a.TextoOriginal).ToArray(),
            pendencias.CategoriasBanco.Select(a => a.TextoOriginal).ToArray());
    }

    private static PreVisualizarFaturaResponse.FinalCartao LerFinal(PendenciasFatura.FinalCartao final)
    {
        return final.Cartao.Match(
            Some: cartao => new PreVisualizarFaturaResponse.FinalCartao(final.Sufixo, Cadastrado: true, cartao.DiaVencimento, cartao.DiaFechamento),
            None: () => new PreVisualizarFaturaResponse.FinalCartao(final.Sufixo, Cadastrado: false, DiaVencimento: null, DiaFechamento: null));
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
    /// O que interrompe a prévia, num tipo só para a cadeia: o arquivo que o leitor recusou (mensagem em texto).
    /// </summary>
    private abstract record Falha
    {
        public static Falha ArquivoInvalido(string mensagem) => new Arquivo(mensagem);

        public sealed record Arquivo(string Mensagem) : Falha;
    }
}
