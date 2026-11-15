using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.Despesas;

/// <summary>
/// Os candidatos a pagamento de fatura do usuário atual, pelo <see cref="CandidatosPagamentoFaturaService"/>, calculados
/// na hora a partir do que está gravado. Chamado depois do commit de uma importação, porque as consultas não enxergam o
/// que só foi adicionado ao escopo. Nada é gravado.
/// </summary>
public sealed class CandidatosPagamentoFaturaAppService
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IImportacaoRepository _importacaoRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IDespesaRepository _despesaRepository;

    public CandidatosPagamentoFaturaAppService(
        IUsuarioAtual usuarioAtual,
        ICartaoRepository cartaoRepository,
        IImportacaoRepository importacaoRepository,
        ITransacaoRepository transacaoRepository,
        IDespesaRepository despesaRepository)
    {
        _usuarioAtual = usuarioAtual;
        _cartaoRepository = cartaoRepository;
        _importacaoRepository = importacaoRepository;
        _transacaoRepository = transacaoRepository;
        _despesaRepository = despesaRepository;
    }

    /// <summary>
    /// Os candidatos cuja saída é da conta e do período do extrato importado, nas faturas cuja janela de pagamento alcança
    /// o período, pelo <see cref="VencimentoFatura.MesesVencimentoFaturasPagaveisEm"/>.
    /// </summary>
    public async Task<IReadOnlyList<CandidatoPagamentoFaturaDto>> ListarCandidatosExtratoAsync(ExtratoImportadoDto extrato, CancellationToken ct)
    {
        IReadOnlyList<CandidatoPagamentoFaturaDto> candidatos = await ApontarAsync(VencimentoFatura.MesesVencimentoFaturasPagaveisEm(extrato.Periodo), ct);

        return candidatos
            .Where(candidato => candidato.Saida.Origem.IdConta == extrato.IdConta && extrato.Periodo.Contem(candidato.Saida.Data))
            .ToList();
    }

    /// <summary>Os candidatos da fatura importada: só o mês do vencimento dela, e só as faturas com um cartão importado.</summary>
    public async Task<IReadOnlyList<CandidatoPagamentoFaturaDto>> ListarCandidatosFaturaImportadaAsync(FaturaImportadaDto fatura, CancellationToken ct)
    {
        IReadOnlyList<CandidatoPagamentoFaturaDto> candidatos = await ApontarAsync([fatura.Vencimento.Mes], ct);

        return candidatos
            .Where(candidato => fatura.Cartoes.Any(cartao => candidato.Candidato.FaturaCartaoAberta.Fatura.Contem(cartao.IdCartao)))
            .ToList();
    }

    /// <summary>Os candidatos das faturas que vencem no mês.</summary>
    public async Task<IReadOnlyList<CandidatoPagamentoFaturaDto>> ListarCandidatosMesVencimentoAsync(MesReferencia mesVencimento, CancellationToken ct) =>
        await ApontarAsync([mesVencimento], ct);

    private async Task<CandidatoPagamentoFaturaDto[]> ApontarAsync(IReadOnlyList<MesReferencia> mesesVencimento, CancellationToken ct)
    {
        IReadOnlyList<Cartao> cartoes = await _cartaoRepository.ListarAsync(ct);
        List<ImportacaoArquivo> importacoes = [];
        List<Transacao> transacoes = [];
        List<Despesa> despesasMeses = [];

        // As faturas dos meses vizinhos competem pelas mesmas saídas, e o par único confere também a elas.
        foreach (MesReferencia mes in CandidatosPagamentoFaturaService.MesesConferidos(mesesVencimento))
        {
            importacoes.AddRange(await _importacaoRepository.ListarImportacoesFaturaEmAsync(mes, ct));
            transacoes.AddRange(await _transacaoRepository.ListarTransacoesFaturaEmAsync(mes, ct));
            despesasMeses.AddRange(await _despesaRepository.ListarDoMesAsync(mes, ct));
        }

        IEnumerable<MesReferencia> mesesSaidas = mesesVencimento
            .SelectMany(VencimentoFatura.MesesPagamentoFaturasVencendoEm)
            .Distinct();
        foreach (MesReferencia mes in mesesSaidas)
            transacoes.AddRange(await _transacaoRepository.ListarDoMesAsync(mes, ct));

        Dictionary<IdTransacao, Transacao> transacoesPorId = transacoes.DistinctBy(t => t.Id).ToDictionary(t => t.Id);
        IdTransacao[] idsSaidas = transacoesPorId.Values.Where(t => t.EhSaidaConta).Select(t => t.Id).ToArray();
        IReadOnlyList<Despesa> despesasPagasSaidas = await _despesaRepository.ListarPagasPorAsync(idsSaidas, ct);

        return CandidatosPagamentoFaturaService
            .Apontar(
                _usuarioAtual.Id,
                mesesVencimento,
                cartoes,
                importacoes.DistinctBy(i => i.Id).ToList(),
                transacoesPorId.Values,
                despesasMeses.DistinctBy(d => d.Id).ToList(),
                despesasPagasSaidas)
            .Select(candidato => new CandidatoPagamentoFaturaDto(candidato, transacoesPorId[candidato.IdSaida]))
            .ToArray();
    }
}
