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
    private readonly IImportacaoFaturaRepository _importacaoFaturaRepository;
    private readonly ITransacaoCartaoRepository _transacaoCartaoRepository;
    private readonly ITransacaoContaRepository _transacaoContaRepository;
    private readonly IDespesaRepository _despesaRepository;

    public CandidatosPagamentoFaturaAppService(
        IUsuarioAtual usuarioAtual,
        ICartaoRepository cartaoRepository,
        IImportacaoFaturaRepository importacaoFaturaRepository,
        ITransacaoCartaoRepository transacaoCartaoRepository,
        ITransacaoContaRepository transacaoContaRepository,
        IDespesaRepository despesaRepository)
    {
        _usuarioAtual = usuarioAtual;
        _cartaoRepository = cartaoRepository;
        _importacaoFaturaRepository = importacaoFaturaRepository;
        _transacaoCartaoRepository = transacaoCartaoRepository;
        _transacaoContaRepository = transacaoContaRepository;
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
            .Where(candidato => candidato.Saida.IdConta == extrato.IdConta && extrato.Periodo.Contem(candidato.Saida.Data))
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
        List<ImportacaoFatura> importacoes = [];
        List<TransacaoCartao> transacoesFaturas = [];
        List<TransacaoConta> saidasConta = [];
        List<Despesa> despesasMeses = [];

        // As faturas dos meses vizinhos competem pelas mesmas saídas, e o par único confere também a elas.
        foreach (MesReferencia mes in CandidatosPagamentoFaturaService.MesesConferidos(mesesVencimento))
        {
            importacoes.AddRange(await _importacaoFaturaRepository.ListarEmAsync(mes, ct));
            transacoesFaturas.AddRange(await _transacaoCartaoRepository.ListarFaturaEmAsync(mes, ct));
            despesasMeses.AddRange(await _despesaRepository.ListarDoMesAsync(mes, ct));
        }

        IEnumerable<MesReferencia> mesesSaidas = mesesVencimento
            .SelectMany(VencimentoFatura.MesesPagamentoFaturasVencendoEm)
            .Distinct();
        foreach (MesReferencia mes in mesesSaidas)
            saidasConta.AddRange(await _transacaoContaRepository.ListarDoMesAsync(mes, ct));

        Dictionary<IdTransacao, TransacaoConta> saidasPorId = saidasConta.DistinctBy(t => t.Id).ToDictionary(t => t.Id);
        IdTransacao[] idsSaidas = saidasPorId.Values.Where(t => t.EhSaida).Select(t => t.Id).ToArray();
        IReadOnlyList<Despesa> despesasPagasSaidas = await _despesaRepository.ListarPagasPorAsync(idsSaidas, ct);

        return CandidatosPagamentoFaturaService
            .Apontar(
                _usuarioAtual.Id,
                mesesVencimento,
                cartoes,
                importacoes.DistinctBy(i => i.Id).ToList(),
                transacoesFaturas.DistinctBy(t => t.Id).ToList(),
                saidasPorId.Values,
                despesasMeses.DistinctBy(d => d.Id).ToList(),
                despesasPagasSaidas)
            .Select(candidato => new CandidatoPagamentoFaturaDto(candidato, saidasPorId[candidato.IdSaida]))
            .ToArray();
    }
}
