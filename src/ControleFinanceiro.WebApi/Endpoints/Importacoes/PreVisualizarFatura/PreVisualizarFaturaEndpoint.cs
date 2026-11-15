using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.PreVisualizarFatura;

/// <summary>
/// Lê a fatura no layout da rota e devolve o que o usuário precisa preencher ou resolver antes de importá-la: os finais
/// de cartão, marcando os já cadastrados, os beneficiários novos e as categorias do banco sem alias. Recusa a fatura cujo
/// vencimento não fecha com as compras ou que já foi importada com outro vencimento no mesmo mês. Nada é gravado, e o
/// arquivo não fica no servidor.
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
    private readonly ImportacaoFaturaService _importacaoFatura;

    public PreVisualizarFaturaEndpoint(
        IUsuarioAtual usuarioAtual,
        LeituraFaturaService leituraFatura,
        PendenciasImportacaoService pendencias,
        ImportacaoFaturaService importacaoFatura)
    {
        _usuarioAtual = usuarioAtual;
        _leituraFatura = leituraFatura;
        _pendencias = pendencias;
        _importacaoFatura = importacaoFatura;
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

        await LerAsync(layout, req, ct)
            .Bind(ConferirVencimento)
            .Bind(fatura => ConferirVencimentoImportadoAsync(idUsuario, fatura, ct))
            .Map(fatura => PreVisualizarAsync(idUsuario, fatura, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(layout, falha, ct));
    }

    private async Task<Result<Fatura, Falha>> LerAsync(LayoutArquivoImportacao layout, PreVisualizarFaturaRequest req, CancellationToken ct)
    {
        await using Stream conteudo = req.Arquivo!.OpenReadStream();

        return await _leituraFatura
            .LerAsync(layout, conteudo, VencimentoDoLayout(layout, req), ct)
            .MapError(Falha.ArquivoInvalido);
    }

    /// <summary>O validator já garantiu o vencimento no layout de vencimento informado; nos outros, ele não vale.</summary>
    private static Maybe<VencimentoFatura> VencimentoDoLayout(LayoutArquivoImportacao layout, PreVisualizarFaturaRequest req)
    {
        return LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout)
            ? (VencimentoFatura)req.DataVencimento!.Value
            : Maybe<VencimentoFatura>.None;
    }

    private static Result<Fatura, Falha> ConferirVencimento(Fatura fatura)
    {
        return fatura
            .ConferirVencimento()
            .Map(() => fatura)
            .MapError(Falha.DeDominio);
    }

    private async Task<Result<Fatura, Falha>> ConferirVencimentoImportadoAsync(IdUsuario idUsuario, Fatura fatura, CancellationToken ct)
    {
        return await _importacaoFatura
            .ConferirVencimentoImportadoAsync(idUsuario, fatura, ct)
            .Map(() => fatura)
            .MapError(Falha.DeDominio);
    }

    private async Task<PreVisualizarFaturaResponse> PreVisualizarAsync(IdUsuario idUsuario, Fatura fatura, CancellationToken ct)
    {
        PendenciasFatura pendencias = await _pendencias.ListarDaFaturaAsync(idUsuario, fatura, ct);

        return new PreVisualizarFaturaResponse(
            fatura.Vencimento,
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

    private async Task EnviarFalhaAsync(LayoutArquivoImportacao layout, Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Arquivo(string mensagem):
                AddError(r => r.Arquivo, mensagem);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case Falha.Dominio(ErroVencimentoIncompativelComFatura or ErroFaturaJaImportadaComOutroVencimento) vencimento
                when LayoutArquivoImportacao.FaturasVencimentoInformado.Contains(layout):
                AddError(r => r.DataVencimento, vencimento.Erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case Falha.Dominio(ErroVencimentoIncompativelComFatura or ErroFaturaJaImportadaComOutroVencimento) vencimento:
                AddError(r => r.Arquivo, vencimento.Erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha.GetType().Name}");
        }
    }

    /// <summary>
    /// O que interrompe a prévia, num tipo só para a cadeia: o arquivo que o leitor recusou (mensagem em texto) e os erros
    /// tipados do domínio, o vencimento que não fecha com as compras e a fatura já importada com outro vencimento.
    /// </summary>
    private abstract record Falha
    {
        public static Falha ArquivoInvalido(FalhaLeituraArquivo falha) => new Arquivo(falha.Mensagem);

        public static Falha DeDominio(Erro erro) => new Dominio(erro);

        public sealed record Arquivo(string Mensagem) : Falha;

        public sealed record Dominio(Erro Erro) : Falha;
    }
}
