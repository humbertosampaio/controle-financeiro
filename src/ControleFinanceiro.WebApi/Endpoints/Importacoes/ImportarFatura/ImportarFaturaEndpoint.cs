using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Lê a fatura no layout da rota e a importa pelo <see cref="ImportacaoFaturaService"/>. Nada é gravado quando a
/// resposta é de erro.
/// </summary>
public sealed class ImportarFaturaEndpoint : Endpoint<ImportarFaturaRequest, ImportarFaturaResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para os outros campos e
    /// os cabeçalhos do multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = ImportarFaturaValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioAtual _usuarioAtual;
    private readonly LeituraFaturaService _leituraFatura;
    private readonly ImportacaoFaturaService _importacaoFatura;

    public ImportarFaturaEndpoint(
        IUsuarioAtual usuarioAtual,
        LeituraFaturaService leituraFatura,
        ImportacaoFaturaService importacaoFatura)
    {
        _usuarioAtual = usuarioAtual;
        _leituraFatura = leituraFatura;
        _importacaoFatura = importacaoFatura;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes/faturas/{layout}");
        AllowFileUploads();
        AllowAnonymous();

        // Sem o limite na rota, o formulário é lido inteiro, e acima de 64 KB guardado em disco, antes de o validator
        // recusar o arquivo. Acima do limite, a leitura para e a resposta é um 400 geral, sem chegar ao validator.
        Options(rota => rota.WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = TamanhoMaximoFormulario }));
    }

    public override async Task HandleAsync(ImportarFaturaRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = _usuarioAtual.Id;
        LayoutArquivoImportacao layout = LayoutArquivoImportacao.FromValue(req.Layout);

        await LerAsync(layout, req.Arquivo!, ct)
            .Bind(fatura => _importacaoFatura
                .ImportarAsync(idUsuario, layout, fatura, ct)
                .MapError(Falha.DeDominio))
            .Map(Responder)
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

    private static ImportarFaturaResponse Responder(FaturaImportada importada)
    {
        return new ImportarFaturaResponse(importada.Cartoes
            .Select(cartao => new ImportarFaturaResponse.CartaoImportado(
                cartao.IdImportacao,
                cartao.IdCartao,
                cartao.Sufixo,
                cartao.Lidas,
                cartao.Novas,
                cartao.JaExistentes,
                cartao.DespesasParceladasCriadas,
                cartao.DespesasAdiadasCriadas))
            .ToArray());
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Arquivo(string mensagem):
                AddError(r => r.Arquivo, mensagem);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case Falha.Dominio(ErroCartoesNaoResolvidos erro):
                AddError(r => r.Arquivo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case Falha.Dominio(ErroBeneficiariosNaoResolvidos erro):
                AddError(r => r.Arquivo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            case Falha.Dominio(ErroCategoriasBancoNaoResolvidas erro):
                AddError(r => r.Arquivo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
                break;

            // ErroOrigemNaoPertenceAoUsuario é bug de quem escolheu o cartão, e cai aqui: 500. A identidade da fatura é
            // composta, então o ErroIdentificadorRepetido não acontece. A mensagem leva só o código, e não a descrição,
            // que vai para o log.
            case Falha.Dominio(Erro erro):
                throw new InvalidOperationException($"Erro de domínio sem status mapeado: {erro.Codigo}");

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha.GetType().Name}");
        }
    }

    /// <summary>
    /// O que interrompe a importação, num tipo só para a cadeia: o arquivo que o leitor recusou (mensagem em texto) e os
    /// erros tipados do domínio.
    /// </summary>
    private abstract record Falha
    {
        public static Falha ArquivoInvalido(FalhaLeituraArquivo falha) => new Arquivo(falha.Mensagem);

        public static Falha DeDominio(Erro erro) => new Dominio(erro);

        public sealed record Arquivo(string Mensagem) : Falha;

        public sealed record Dominio(Erro Erro) : Falha;
    }
}
