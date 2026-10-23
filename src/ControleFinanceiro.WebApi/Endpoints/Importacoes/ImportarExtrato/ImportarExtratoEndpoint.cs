using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarExtrato;

/// <summary>
/// Lê o arquivo, confere os saldos, obtém ou cria a conta e grava só as transações novas, junto com a conta
/// nova e o registro da importação, num commit só. Nada é gravado quando a resposta é de erro.
/// </summary>
public sealed class ImportarExtratoEndpoint : Endpoint<ImportarExtratoRequest, ImportarExtratoResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para o outro campo e
    /// os cabeçalhos do multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = ImportarExtratoValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly IEnumerable<ILeitorExtrato> _leitores;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportarExtratoEndpoint(
        IUsuarioRepository usuarioRepository,
        IContaRepository contaRepository,
        ITransacaoRepository transacaoRepository,
        IImportacaoRepository importacaoRepository,
        IEnumerable<ILeitorExtrato> leitores,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _contaRepository = contaRepository;
        _transacaoRepository = transacaoRepository;
        _importacaoRepository = importacaoRepository;
        _leitores = leitores;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
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
        FormatoExtrato formato = FormatoExtrato.FromValue(req.Formato);

        await _usuarioRepository.ObterPorIdAsync(idUsuario, ct)
            .ToResult(Falha.UsuarioNaoEncontrado)
            .Bind(_ => LerAsync(formato, req.Arquivo!, ct))
            .Bind(ConferirSaldo)
            .Bind(extrato => ImportarAsync(idUsuario, formato, extrato, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    private async Task<Result<Extrato, Falha>> LerAsync(FormatoExtrato formato, IFormFile arquivo, CancellationToken ct)
    {
        ILeitorExtrato leitor = _leitores.Single(l => l.Formato == formato);
        await using Stream conteudo = arquivo.OpenReadStream();

        return await leitor
            .LerAsync(conteudo, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    private static Result<Extrato, Falha> ConferirSaldo(Extrato extrato)
    {
        return ConferenciaSaldo.Conferir(extrato)
            .Map(() => extrato)
            .MapError(Falha.ArquivoInvalido);
    }

    /// <summary>
    /// Obtém a conta, registra a importação e monta o lote; só grava se nenhum deles falhar. Cada etapa fica
    /// dentro da anterior para enxergar o que ela produziu.
    /// </summary>
    private async Task<Result<ImportarExtratoResponse, Falha>> ImportarAsync(
        IdUsuario idUsuario,
        FormatoExtrato formato,
        Extrato extrato,
        CancellationToken ct)
    {
        IReadOnlyList<ContaBancaria> contasDoUsuario = await _contaRepository.ListarDoUsuarioAsync(idUsuario, ct);

        return await ContaBancariaFactory
            .ObterOuCriar(idUsuario, contasDoUsuario, extrato.Banco, extrato.TipoConta, extrato.SufixoConta)
            .MapError(Falha.DeDominio)
            .Bind(conta =>
            {
                ImportacaoExtrato importacao = ImportacaoExtratoFactory.Registrar(
                    conta,
                    formato,
                    extrato.Periodo,
                    extrato.LancamentosContabilizados.Count,
                    _timeProvider.GetUtcNow());

                return LoteTransacoesFactory
                    .Criar(idUsuario, conta, importacao, extrato.Lancamentos)
                    .MapError(Falha.DeDominio)
                    .Map(transacoes => GravarAsync(extrato, contasDoUsuario, conta, importacao, transacoes, ct));
            });
    }

    private async Task<ImportarExtratoResponse> GravarAsync(
        Extrato extrato,
        IReadOnlyList<ContaBancaria> contasDoUsuario,
        ContaBancaria conta,
        ImportacaoExtrato importacao,
        IReadOnlyList<Transacao> transacoes,
        CancellationToken ct)
    {
        OrigemTransacao origem = importacao.Origem;

        Maybe<ImportacaoExtrato> importacaoAnterior = await _importacaoRepository.ObterMaisRecenteAsync(origem, ct);

        // Lidas antes de adicionar as novas: a consulta só traz o que já estava gravado.
        IReadOnlyList<Transacao> transacoesExistentesNoPeriodo = await _transacaoRepository.ListarNoPeriodoAsync(origem, extrato.Periodo, ct);

        if (!contasDoUsuario.Contains(conta))
            await _contaRepository.AdicionarAsync(conta, ct);

        await _importacaoRepository.AdicionarAsync(importacao, ct);
        IReadOnlyList<Transacao> novas = await _transacaoRepository.AdicionarNovasAsync(transacoes, ct);

        Result somaDoPeriodo = ConferenciaSomaPeriodo.Conferir(extrato, transacoesExistentesNoPeriodo, novas);
        Maybe<PeriodoExtrato> periodoNaoImportado = importacao.PeriodoNaoImportado(importacaoAnterior);

        await _unitOfWork.CommitAsync(ct);

        return new ImportarExtratoResponse(
            importacao.Id,
            conta.Id,
            importacao.Lidas,
            novas.Count,
            importacao.Lidas - novas.Count,
            periodoNaoImportado.Map(p => new ImportarExtratoResponse.Periodo(p.Inicio, p.Fim)).GetValueOrDefault(),
            somaDoPeriodo.IsFailure ? somaDoPeriodo.Error : null);
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
    /// O que interrompe a importação, num tipo só para a cadeia: o usuário inexistente, o arquivo que o leitor ou a
    /// conferência de saldo recusou (mensagem em texto) e os erros tipados do domínio.
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
