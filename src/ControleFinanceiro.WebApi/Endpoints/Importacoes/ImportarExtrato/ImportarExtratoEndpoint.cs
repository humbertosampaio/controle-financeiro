using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
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
/// Lê o extrato no layout da rota e o importa pelo <see cref="ImportacaoExtratoService"/>. O layout cujo arquivo não
/// traz a conta é lido com o sufixo que o usuário informou. Nada é gravado quando a resposta é de erro.
/// </summary>
public sealed class ImportarExtratoEndpoint : Endpoint<ImportarExtratoRequest, ImportarExtratoResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para o outro campo e
    /// os cabeçalhos do multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = ImportarExtratoValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioAtual _usuarioAtual;
    private readonly LeituraExtratoService _leituraExtrato;
    private readonly ImportacaoExtratoService _importacaoExtrato;

    public ImportarExtratoEndpoint(
        IUsuarioAtual usuarioAtual,
        LeituraExtratoService leituraExtrato,
        ImportacaoExtratoService importacaoExtrato)
    {
        _usuarioAtual = usuarioAtual;
        _leituraExtrato = leituraExtrato;
        _importacaoExtrato = importacaoExtrato;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes/extratos/{layout}");
        AllowFileUploads();
        AllowAnonymous();

        // Sem o limite na rota, o formulário é lido inteiro, e acima de 64 KB guardado em disco, antes de o validator
        // recusar o arquivo. Acima do limite, a leitura para e a resposta é um 400 geral, sem chegar ao validator.
        Options(rota => rota.WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = TamanhoMaximoFormulario }));
    }

    public override async Task HandleAsync(ImportarExtratoRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = _usuarioAtual.Id;
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(req.Layout);

        await LerAsync(layout, req, ct)
            .Bind(extrato => _importacaoExtrato
                .ImportarAsync(idUsuario, layout, extrato, ct)
                .MapError(Falha.DeDominio))
            .Map(Responder)
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(layout, falha, ct));
    }

    /// <param name="req">Já validada: traz o sufixo quando o layout pede.</param>
    private async Task<Result<Extrato, Falha>> LerAsync(LayoutArquivoImportacao layout, ImportarExtratoRequest req, CancellationToken ct)
    {
        await using Stream conteudo = req.Arquivo!.OpenReadStream();

        return await _leituraExtrato
            .LerAsync(layout, conteudo, req.SufixoConta, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    private static ImportarExtratoResponse Responder(ExtratoImportado importado)
    {
        return new ImportarExtratoResponse(
            importado.IdImportacao,
            importado.IdConta,
            importado.Lidas,
            importado.Novas,
            importado.JaExistentes,
            importado.PeriodoNaoImportado.Map(p => new ImportarExtratoResponse.Periodo(p.Inicio, p.Fim)).GetValueOrDefault(),
            importado.SomaPeriodo.IsFailure ? importado.SomaPeriodo.Error : null);
    }

    private async Task EnviarFalhaAsync(LayoutArquivoImportacao layout, Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Arquivo(string mensagem):
                await EnviarArquivoInvalidoAsync(mensagem, ct);
                break;

            // A conta vem do sufixo quando o usuário o informou, e do arquivo nos outros layouts.
            case Falha.Dominio(ErroContaDeOutroTipo erro) when LayoutArquivoImportacao.ExtratosContaInformada.Contains(layout):
                AddError(r => r.SufixoConta, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            case Falha.Dominio(ErroContaDeOutroTipo erro):
                AddError(r => r.Arquivo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            case Falha.Dominio(ErroIdentificadorRepetido erro):
                await EnviarArquivoInvalidoAsync(erro.Descricao, ct);
                break;

            case Falha.Dominio(ErroBeneficiariosNaoResolvidos erro):
                await EnviarArquivoInvalidoAsync(erro.Descricao, ct);
                break;

            // Nenhum leitor de extrato preenche a categoria do banco hoje; o case evita o 500 num layout futuro que preencha.
            case Falha.Dominio(ErroCategoriasBancoNaoResolvidas erro):
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
    /// O que interrompe a importação, num tipo só para a cadeia: o arquivo que o leitor recusou, inclusive pelo saldo
    /// que não fecha (mensagem em texto), e os erros tipados do domínio.
    /// </summary>
    private abstract record Falha
    {
        public static Falha ArquivoInvalido(string mensagem) => new Arquivo(mensagem);

        public static Falha DeDominio(Erro erro) => new Dominio(erro);

        public sealed record Arquivo(string Mensagem) : Falha;

        public sealed record Dominio(Erro Erro) : Falha;
    }
}
