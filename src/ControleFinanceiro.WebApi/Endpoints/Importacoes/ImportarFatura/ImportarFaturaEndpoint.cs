using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using LoteCartao = (ControleFinanceiro.Domain.Cartoes.Cartao Cartao, ControleFinanceiro.Domain.Importacoes.ImportacaoArquivo Importacao, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Transacoes.Transacao> Transacoes);

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Lê a fatura, obtém ou cria um cartão por final e grava, de cada cartão, a importação e só as transações novas,
/// junto com os cartões novos, num commit só. Nada é gravado quando a resposta é de erro.
/// </summary>
public sealed class ImportarFaturaEndpoint : Endpoint<ImportarFaturaRequest, ImportarFaturaResponse>
{
    /// <summary>
    /// O maior formulário que a rota lê: o arquivo no tamanho máximo do validator, com folga para os outros campos e
    /// os cabeçalhos do multipart.
    /// </summary>
    public const long TamanhoMaximoFormulario = ImportarFaturaValidator.TamanhoMaximoArquivo + 1024 * 1024;

    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly IEnumerable<ILeitorFatura> _leitores;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportarFaturaEndpoint(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarioRepository,
        ICartaoRepository cartaoRepository,
        ITransacaoRepository transacaoRepository,
        IImportacaoRepository importacaoRepository,
        IEnumerable<ILeitorFatura> leitores,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _usuarioRepository = usuarioRepository;
        _cartaoRepository = cartaoRepository;
        _transacaoRepository = transacaoRepository;
        _importacaoRepository = importacaoRepository;
        _leitores = leitores;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/importacoes/faturas");
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
        VencimentosCartoesNovos vencimentos = LerVencimentos(req);

        await _usuarioRepository.ObterPorIdAsync(idUsuario, ct)
            .ToResult(Falha.UsuarioNaoEncontrado)
            .Bind(_ => LerAsync(layout, req.Arquivo!, ct))
            .Bind(fatura => ImportarAsync(idUsuario, layout, fatura, vencimentos, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    /// <param name="req">Já validada.</param>
    private static VencimentosCartoesNovos LerVencimentos(ImportarFaturaRequest req)
    {
        DiaVencimentoCartao? diaUnico = req.DiaVencimento is int dia ? (DiaVencimentoCartao)dia : null;
        return VencimentosFormulario.Ler(diaUnico, req.Vencimentos).Value;
    }

    private async Task<Result<Fatura, Falha>> LerAsync(LayoutArquivoImportacao layout, IFormFile arquivo, CancellationToken ct)
    {
        ILeitorFatura leitor = _leitores.Single(l => l.Layout == layout);
        await using Stream conteudo = arquivo.OpenReadStream();

        return await leitor
            .LerAsync(conteudo, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    /// <summary>Obtém os cartões e monta o lote de cada um; só grava se nenhum lote falhar.</summary>
    private async Task<Result<ImportarFaturaResponse, Falha>> ImportarAsync(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        VencimentosCartoesNovos vencimentos,
        CancellationToken ct)
    {
        IReadOnlyList<Cartao> cartoesDoUsuario = await _cartaoRepository.ListarAsync(ct);
        IReadOnlyList<Cartao> cartoes = CartaoFactory.ObterOuCriar(idUsuario, cartoesDoUsuario, fatura.Banco, fatura.Sufixos, vencimentos);

        return await MontarLotes(idUsuario, layout, fatura, cartoes)
            .MapError(Falha.DeDominio)
            .Map(lotes => GravarAsync(cartoesDoUsuario, vencimentos, lotes, ct));
    }

    /// <summary>Para no primeiro lote que falhar.</summary>
    private Result<IReadOnlyList<LoteCartao>, Erro> MontarLotes(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        IReadOnlyList<Cartao> cartoes)
    {
        DateTimeOffset importadaEm = _timeProvider.GetUtcNow();
        List<LoteCartao> lotes = [];

        foreach (Cartao cartao in cartoes)
        {
            Result<LoteCartao, Erro> lote = MontarLote(idUsuario, layout, fatura, cartao, importadaEm);
            if (lote.IsFailure)
                return lote.Error;

            lotes.Add(lote.Value);
        }

        return lotes;
    }

    private static Result<LoteCartao, Erro> MontarLote(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        Cartao cartao,
        DateTimeOffset importadaEm)
    {
        IReadOnlyList<Lancamento> lancamentos = fatura.LancamentosCartao(cartao.Sufixo);
        ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(
            cartao,
            layout,
            fatura.PeriodoCartao(cartao.Sufixo),
            lancamentos.Count,
            importadaEm);

        return LoteTransacoesFactory
            .Criar(idUsuario, cartao, importacao, lancamentos)
            .Map(transacoes => (cartao, importacao, transacoes));
    }

    private async Task<ImportarFaturaResponse> GravarAsync(
        IReadOnlyList<Cartao> cartoesDoUsuario,
        VencimentosCartoesNovos vencimentos,
        IReadOnlyList<LoteCartao> lotes,
        CancellationToken ct)
    {
        List<ImportarFaturaResponse.CartaoImportado> cartoesImportados = [];

        foreach ((Cartao cartao, ImportacaoArquivo importacao, IReadOnlyList<Transacao> transacoes) in lotes)
        {
            bool cartaoNovo = !cartoesDoUsuario.Contains(cartao);
            if (cartaoNovo)
                await _cartaoRepository.AdicionarAsync(cartao, ct);

            await _importacaoRepository.AdicionarAsync(importacao, ct);
            IReadOnlyList<Transacao> novas = await _transacaoRepository.AdicionarNovasAsync(transacoes, ct);

            cartoesImportados.Add(new ImportarFaturaResponse.CartaoImportado(
                importacao.Id,
                cartao.Id,
                cartao.Sufixo,
                cartaoNovo,
                cartaoNovo && vencimentos.EhPadrao(cartao.Sufixo),
                importacao.Lidas,
                novas.Count,
                importacao.Lidas - novas.Count));
        }

        await _unitOfWork.CommitAsync(ct);

        return new ImportarFaturaResponse(cartoesImportados);
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.NaoEncontrado:
                await Send.NotFoundAsync(ct);
                break;

            case Falha.Arquivo(string mensagem):
                AddError(r => r.Arquivo, mensagem);
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
    /// O que interrompe a importação, num tipo só para a cadeia: o usuário inexistente, o arquivo que o leitor recusou
    /// (mensagem em texto) e os erros tipados do domínio.
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
