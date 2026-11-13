using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using LoteConta = (ControleFinanceiro.Domain.Contas.ContaBancaria Conta, ControleFinanceiro.Domain.Importacoes.ImportacaoArquivo Importacao, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Transacoes.Transacao> Transacoes);

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O que a importação de um extrato faz depois de ler o arquivo, em qualquer rota: obtém ou cria a conta, registra a
/// importação, monta o lote, confere que todo texto de beneficiário e toda categoria do banco do arquivo já são alias
/// e acrescenta só as transações novas, junto com a conta nova e a importação. O <see cref="AplicarAsync"/> deixa tudo
/// no escopo sem gravar, para a prévia que descarta o escopo; o <see cref="ImportarAsync"/> grava num commit só. Nada é
/// gravado quando o resultado é de erro.
/// </summary>
public sealed class ImportacaoExtratoService
{
    private readonly IContaRepository _contaRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportacaoExtratoService(
        IContaRepository contaRepository,
        IBeneficiarioRepository beneficiarioRepository,
        ICategoriaDespesaRepository categoriaDespesaRepository,
        ITransacaoRepository transacaoRepository,
        IImportacaoRepository importacaoRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository;
        _beneficiarioRepository = beneficiarioRepository;
        _categoriaDespesaRepository = categoriaDespesaRepository;
        _transacaoRepository = transacaoRepository;
        _importacaoRepository = importacaoRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Aplica a importação e grava, num commit só, se nada falhar.</summary>
    /// <param name="idUsuario">Um usuário que existe.</param>
    public async Task<Result<ExtratoImportado, Erro>> ImportarAsync(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Extrato extrato,
        CancellationToken ct)
    {
        return await AplicarAsync(idUsuario, layout, extrato, ct)
            .Tap(() => _unitOfWork.CommitAsync(ct));
    }

    /// <summary>
    /// Obtém a conta, registra a importação e monta o lote; só acrescenta ao escopo se nenhum deles falhar. Não grava.
    /// </summary>
    /// <param name="idUsuario">Um usuário que existe.</param>
    public async Task<Result<ExtratoImportado, Erro>> AplicarAsync(
        IdUsuario idUsuario,
        LayoutArquivoImportacao layout,
        Extrato extrato,
        CancellationToken ct)
    {
        IReadOnlyList<ContaBancaria> contasDoUsuario = await _contaRepository.ListarAsync(ct);
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario = await _beneficiarioRepository.ListarAsync(ct);
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario = await _categoriaDespesaRepository.ListarAsync(ct);

        return await ContaBancariaFactory
            .ObterOuCriar(idUsuario, contasDoUsuario, extrato.Banco, extrato.TipoConta, extrato.SufixoConta)
            .Bind(conta => MontarLote(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, layout, extrato, conta))
            .Map(lote => AcrescentarAsync(extrato, contasDoUsuario, lote, ct));
    }

    /// <summary>Registra a importação do arquivo na conta e monta o lote das transações ligadas a ela.</summary>
    private Result<LoteConta, Erro> MontarLote(
        IdUsuario idUsuario,
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario,
        IReadOnlyList<CategoriaDespesa> categoriasDoUsuario,
        LayoutArquivoImportacao layout,
        Extrato extrato,
        ContaBancaria conta)
    {
        ImportacaoArquivo importacao = ImportacaoArquivoFactory.Registrar(
            conta,
            layout,
            extrato.Periodo,
            extrato.LancamentosContabilizados.Count,
            _timeProvider.GetUtcNow());

        return LoteTransacoesFactory
            .Criar(idUsuario, beneficiariosDoUsuario, categoriasDoUsuario, conta, importacao, extrato.Lancamentos)
            .Map(transacoes => (conta, importacao, transacoes));
    }

    private async Task<ExtratoImportado> AcrescentarAsync(
        Extrato extrato,
        IReadOnlyList<ContaBancaria> contasDoUsuario,
        LoteConta lote,
        CancellationToken ct)
    {
        (ContaBancaria conta, ImportacaoArquivo importacao, IReadOnlyList<Transacao> transacoes) = lote;
        OrigemTransacao origem = importacao.Origem;

        Maybe<ImportacaoArquivo> importacaoAnterior = await _importacaoRepository.ObterMaisRecenteAsync(origem, ct);

        // Lidas antes de adicionar as novas: a consulta só traz o que já estava gravado.
        IReadOnlyList<Transacao> transacoesPeriodo = await _transacaoRepository.ListarNoPeriodoAsync(origem, extrato.Periodo, ct);

        bool contaNova = !contasDoUsuario.Contains(conta);
        if (contaNova)
            await _contaRepository.AdicionarAsync(conta, ct);

        await _importacaoRepository.AdicionarAsync(importacao, ct);
        IReadOnlyList<Transacao> novas = await _transacaoRepository.AdicionarNovasAsync(transacoes, ct);

        return new ExtratoImportado(
            importacao.Id,
            conta.Id,
            contaNova,
            conta.Banco,
            conta.Tipo,
            conta.Sufixo,
            extrato.Periodo,
            importacao.Lidas,
            novas.Count,
            importacao.Lidas - novas.Count,
            importacao.PeriodoNaoImportado(importacaoAnterior),
            ConferenciaSomaPeriodo.Conferir(extrato, transacoesPeriodo, novas));
    }
}
