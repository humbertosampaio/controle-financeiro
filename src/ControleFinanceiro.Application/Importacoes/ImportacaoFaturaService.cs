using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using LoteCartao = (ControleFinanceiro.Domain.Cartoes.Cartao Cartao, ControleFinanceiro.Domain.Importacoes.ImportacaoArquivo Importacao, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Transacoes.Transacao> Transacoes, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Despesas.DespesaParcelada> DespesasParceladas);

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O que a importação de uma fatura faz depois de ler o arquivo, em qualquer rota: confere que todo final já tem
/// cartão e que todo texto de beneficiário e toda categoria do banco já são alias e acrescenta, de cada cartão, a
/// importação, só as transações novas e as despesas parceladas que faltam, e, das compras à vista novas sem despesa
/// prevista, as despesas adiadas já cobradas. O <see cref="AplicarAsync"/> deixa tudo no escopo sem gravar, para a
/// prévia que descarta o escopo; o <see cref="ImportarAsync"/> grava num commit só. Nada é gravado quando o resultado é
/// de erro.
/// </summary>
public sealed class ImportacaoFaturaService
{
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;
    private readonly IDespesaRepository _despesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportacaoFaturaService(
        ICartaoRepository cartaoRepository,
        IBeneficiarioRepository beneficiarioRepository,
        ICategoriaDespesaRepository categoriaDespesaRepository,
        IDespesaRepository despesaRepository,
        ITransacaoRepository transacaoRepository,
        IImportacaoRepository importacaoRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _cartaoRepository = cartaoRepository;
        _beneficiarioRepository = beneficiarioRepository;
        _categoriaDespesaRepository = categoriaDespesaRepository;
        _despesaRepository = despesaRepository;
        _transacaoRepository = transacaoRepository;
        _importacaoRepository = importacaoRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Aplica a importação e grava, num commit só, se nada falhar.</summary>
    /// <param name="idUsuario">Um usuário que existe.</param>
    public async Task<Result<FaturaImportada, Erro>> ImportarAsync(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Fatura fatura,
        CancellationToken ct)
    {
        return await AplicarAsync(idUsuario, layout, fatura, ct)
            .Tap(() => _unitOfWork.CommitAsync(ct));
    }

    /// <summary>
    /// Obtém os cartões e monta o lote de cada um; só acrescenta ao escopo se nenhum final ficar sem cartão e nenhum
    /// lote falhar. Não grava.
    /// </summary>
    /// <param name="idUsuario">Um usuário que existe.</param>
    public async Task<Result<FaturaImportada, Erro>> AplicarAsync(
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
            .Map(lotes => AcrescentarAsync(fatura.Banco, lotes, novas => DespesaAdiadaFactory.CriarDaFatura(
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
    private async Task<FaturaImportada> AcrescentarAsync(
        Banco banco,
        IReadOnlyList<LoteCartao> lotes,
        Func<IReadOnlyList<Transacao>, IReadOnlyList<DespesaAdiada>> criarAdiadas,
        CancellationToken ct)
    {
        List<Transacao> novasDaFatura = [];
        List<(LoteCartao Lote, int Novas)> acrescentados = [];

        foreach (LoteCartao lote in lotes)
        {
            await _importacaoRepository.AdicionarAsync(lote.Importacao, ct);
            IReadOnlyList<Transacao> novas = await _transacaoRepository.AdicionarNovasAsync(lote.Transacoes, ct);
            await _despesaRepository.AdicionarAsync(lote.DespesasParceladas, ct);

            novasDaFatura.AddRange(novas);
            acrescentados.Add((lote, novas.Count));
        }

        IReadOnlyList<DespesaAdiada> despesasAdiadas = criarAdiadas(novasDaFatura);
        await _despesaRepository.AdicionarAsync(despesasAdiadas, ct);

        return new FaturaImportada(banco, acrescentados
            .Select(acrescentado => new FaturaImportada.CartaoImportado(
                acrescentado.Lote.Importacao.Id,
                acrescentado.Lote.Cartao.Id,
                acrescentado.Lote.Cartao.Sufixo,
                acrescentado.Lote.Importacao.Lidas,
                acrescentado.Novas,
                acrescentado.Lote.Importacao.Lidas - acrescentado.Novas,
                acrescentado.Lote.DespesasParceladas.Count,
                despesasAdiadas.Count(d => d.IdCartao == acrescentado.Lote.Cartao.Id)))
            .ToArray());
    }
}
