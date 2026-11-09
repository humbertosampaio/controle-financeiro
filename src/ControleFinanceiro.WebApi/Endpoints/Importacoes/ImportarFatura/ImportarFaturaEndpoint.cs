using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using LoteCartao = (ControleFinanceiro.Domain.Cartoes.Cartao Cartao, ControleFinanceiro.Domain.Importacoes.ImportacaoArquivo Importacao, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Transacoes.Transacao> Transacoes, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Despesas.DespesaParcelada> DespesasParceladas);

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Lê a fatura, confere que todo final já tem cartão e que todo texto de beneficiário e toda categoria do banco já são
/// alias e grava, de cada cartão, a importação, só as transações novas e as despesas parceladas que faltam, e, das
/// compras à vista novas sem despesa prevista, as despesas adiadas já cobradas, num commit só. Nada é gravado quando a
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
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly IEnumerable<ILeitorFatura> _leitores;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportarFaturaEndpoint(
        IUsuarioAtual usuarioAtual,
        ICartaoRepository cartaoRepository,
        IBeneficiarioRepository beneficiarioRepository,
        ICategoriaDespesaRepository categoriaDespesaRepository,
        IDespesaRepository despesaRepository,
        ITransacaoRepository transacaoRepository,
        IImportacaoRepository importacaoRepository,
        IEnumerable<ILeitorFatura> leitores,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _cartaoRepository = cartaoRepository;
        _beneficiarioRepository = beneficiarioRepository;
        _categoriaDespesaRepository = categoriaDespesaRepository;
        _despesaRepository = despesaRepository;
        _transacaoRepository = transacaoRepository;
        _importacaoRepository = importacaoRepository;
        _leitores = leitores;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
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
            .Bind(fatura => ImportarAsync(idUsuario, layout, fatura, ct))
            .Match(
                resposta => Send.OkAsync(resposta, ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    private async Task<Result<Fatura, Falha>> LerAsync(LayoutArquivoImportacao layout, IFormFile arquivo, CancellationToken ct)
    {
        ILeitorFatura leitor = _leitores.Single(l => l.Layout == layout);
        await using Stream conteudo = arquivo.OpenReadStream();

        return await leitor
            .LerAsync(conteudo, ct)
            .MapError(Falha.ArquivoInvalido);
    }

    /// <summary>Obtém os cartões e monta o lote de cada um; só grava se nenhum final ficar sem cartão e nenhum lote falhar.</summary>
    private async Task<Result<ImportarFaturaResponse, Falha>> ImportarAsync(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        CancellationToken ct)
    {
        IReadOnlyList<Cartao> cartoesDoUsuario = await _cartaoRepository.ListarAsync(ct);
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario = await _beneficiarioRepository.ListarAsync(ct);
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario = await _categoriaDespesaRepository.ListarAsync(ct);
        IReadOnlyList<DespesaParcelada> parceladasDoUsuario = await _despesaRepository.ListarParceladasAsync(ct);
        IReadOnlyList<DespesaAdiada> adiadasDoUsuario = await _despesaRepository.ListarAdiadasAsync(ct);

        return await CartaoFactory
            .ObterDosFinais(idUsuario, cartoesDoUsuario, fatura.Banco, fatura.Sufixos)
            .Bind(cartoes => MontarLotes(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, parceladasDoUsuario, layout, fatura, cartoes))
            .MapError(Falha.DeDominio)
            .Map(lotes => GravarAsync(lotes, novas => DespesaAdiadaFactory.CriarDaFatura(
                idUsuario,
                beneficiariosDoUsuario,
                categoriasDoUsuario,
                cartoesDoUsuario,
                adiadasDoUsuario,
                novas), ct));
    }

    /// <summary>Para no primeiro lote que falhar.</summary>
    private Result<IReadOnlyList<LoteCartao>, Erro> MontarLotes(
        IdUsuario idUsuario,
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario,
        IReadOnlyList<DespesaParcelada> parceladasDoUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        IReadOnlyList<Cartao> cartoes)
    {
        DateTimeOffset importadaEm = _timeProvider.GetUtcNow();
        List<LoteCartao> lotes = [];

        foreach (Cartao cartao in cartoes)
        {
            Result<LoteCartao, Erro> lote = MontarLote(
                idUsuario,
                beneficiariosDoUsuario,
                categoriasDoUsuario,
                parceladasDoUsuario,
                layout,
                fatura,
                cartao,
                importadaEm);
            if (lote.IsFailure)
                return lote.Error;

            lotes.Add(lote.Value);
        }

        return lotes;
    }

    private static Result<LoteCartao, Erro> MontarLote(
        IdUsuario idUsuario,
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario,
        IReadOnlyList<DespesaParcelada> parceladasDoUsuario,
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
            .Criar(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, cartao, importacao, lancamentos)
            .Map(transacoes =>
            {
                IReadOnlyList<DespesaParcelada> despesasParceladas = DespesaParceladaFactory.CriarDaFatura(
                    idUsuario,
                    beneficiariosDoUsuario,
                    categoriasDoUsuario,
                    cartao,
                    parceladasDoUsuario,
                    lancamentos);

                return (cartao, importacao, transacoes, despesasParceladas);
            });
    }

    /// <param name="criarAdiadas">
    /// Cria as despesas adiadas das transações novas de todos os cartões de uma vez, para a previsão num final cobrir a
    /// compra de outro final da mesma fatura uma vez só.
    /// </param>
    private async Task<ImportarFaturaResponse> GravarAsync(
        IReadOnlyList<LoteCartao> lotes,
        Func<IReadOnlyList<Transacao>, IReadOnlyList<DespesaAdiada>> criarAdiadas,
        CancellationToken ct)
    {
        List<Transacao> novasDaFatura = [];
        List<(LoteCartao Lote, int Novas)> gravados = [];

        foreach (LoteCartao lote in lotes)
        {
            await _importacaoRepository.AdicionarAsync(lote.Importacao, ct);
            IReadOnlyList<Transacao> novas = await _transacaoRepository.AdicionarNovasAsync(lote.Transacoes, ct);
            await _despesaRepository.AdicionarAsync(lote.DespesasParceladas, ct);

            novasDaFatura.AddRange(novas);
            gravados.Add((lote, novas.Count));
        }

        IReadOnlyList<DespesaAdiada> despesasAdiadas = criarAdiadas(novasDaFatura);
        await _despesaRepository.AdicionarAsync(despesasAdiadas, ct);
        await _unitOfWork.CommitAsync(ct);

        return new ImportarFaturaResponse(gravados
            .Select(gravado => new ImportarFaturaResponse.CartaoImportado(
                gravado.Lote.Importacao.Id,
                gravado.Lote.Cartao.Id,
                gravado.Lote.Cartao.Sufixo,
                gravado.Lote.Importacao.Lidas,
                gravado.Novas,
                gravado.Lote.Importacao.Lidas - gravado.Novas,
                gravado.Lote.DespesasParceladas.Count,
                despesasAdiadas.Count(d => d.IdCartao == gravado.Lote.Cartao.Id)))
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
        public static Falha ArquivoInvalido(string mensagem) => new Arquivo(mensagem);

        public static Falha DeDominio(Erro erro) => new Dominio(erro);

        public sealed record Arquivo(string Mensagem) : Falha;

        public sealed record Dominio(Erro Erro) : Falha;
    }
}
