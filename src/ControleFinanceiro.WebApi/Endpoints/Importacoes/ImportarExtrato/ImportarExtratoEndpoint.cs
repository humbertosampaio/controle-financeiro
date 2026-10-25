using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>
/// Lê o arquivo de um layout que traz a conta e o importa pelo <see cref="ImportacaoExtratoService"/>. Nada é gravado
/// quando a resposta é de erro.
/// </summary>
public sealed class ImportarExtratoEndpoint : Endpoint<ImportarExtratoRequest, ImportarExtratoResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para o outro campo e
    /// os cabeçalhos do multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = ImportarExtratoValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEnumerable<ILeitorExtrato> _leitores;
    private readonly ImportacaoExtratoService _importacaoExtrato;

    public ImportarExtratoEndpoint(
        IUsuarioRepository usuarioRepository,
        IEnumerable<ILeitorExtrato> leitores,
        ImportacaoExtratoService importacaoExtrato)
    {
        _usuarioRepository = usuarioRepository;
        _leitores = leitores;
        _importacaoExtrato = importacaoExtrato;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes");
        AllowFileUploads();
        AllowAnonymous();

        // Sem o limite na rota, o formulário é lido inteiro, e acima de 64 KB guardado em disco, antes de o validator
        // recusar o arquivo. Acima do limite, a leitura para e a resposta é um 400 geral, sem chegar ao validator.
        Options(rota => rota.WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = TamanhoMaximoFormulario }));
    }

    public override async Task HandleAsync(ImportarExtratoRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = IdUsuario.From(req.IdUsuario);
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(req.Layout);

        await _usuarioRepository.ObterPorIdAsync(idUsuario, ct)
            .ToResult(Falha.UsuarioNaoEncontrado)
            .Bind(_ => LerAsync(layout, req.Arquivo!, ct))
            .Bind(extrato => _importacaoExtrato
                .ImportarAsync(idUsuario, layout, extrato, ct)
                .MapError(Falha.DeDominio))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    private async Task<Result<Extrato, Falha>> LerAsync(LayoutArquivoImportacao layout, IFormFile arquivo, CancellationToken ct)
    {
        ILeitorExtrato leitor = _leitores.Single(l => l.Layout == layout);
        await using Stream conteudo = arquivo.OpenReadStream();

        return await leitor
            .LerAsync(conteudo, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.NaoEncontrado:
                await Send.NotFoundAsync(ct);
                break;

            case Falha.Arquivo(string mensagem):
                await EnviarArquivoInvalidoAsync(mensagem, ct);
                break;

            case Falha.Dominio(ErroContaDeOutroTipo erro):
                AddError(r => r.Arquivo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            case Falha.Dominio(ErroIdentificadorRepetido erro):
                await EnviarArquivoInvalidoAsync(erro.Descricao, ct);
                break;

            // ErroOrigemNaoPertenceAoUsuario é bug de quem escolheu a conta, e cai aqui: 500. A mensagem leva só o
            // código, e não a descrição, que vai para o log.
            case Falha.Dominio(Erro erro):
                throw new InvalidOperationException($"Erro de domínio sem status mapeado: {erro.Codigo}");

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha.GetType().Name}");
        }
    }

    private async Task EnviarArquivoInvalidoAsync(string mensagem, CancellationToken ct)
    {
        AddError(r => r.Arquivo, mensagem);
        await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
    }

    /// <summary>
    /// O que interrompe a importação, num tipo só para a cadeia: o usuário inexistente, o arquivo que o leitor recusou,
    /// inclusive pelo saldo que não fecha (mensagem em texto), e os erros tipados do domínio.
    /// </summary>
    private abstract record Falha
    {
        public static readonly Falha UsuarioNaoEncontrado = new NaoEncontrado();

        public static Falha ArquivoInvalido(string mensagem) => new Arquivo(mensagem);

        public static Falha DeDominio(Erro erro) => new Dominio(erro);

        public sealed record NaoEncontrado : Falha;

        public sealed record Arquivo(string Mensagem) : Falha;

        public sealed record Dominio(Erro Erro) : Falha;
    }
}
