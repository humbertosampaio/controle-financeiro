using ControleFinanceiro.Application.SaidaModelo;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Leituras;

/// <summary>
/// As leituras que vão ao modelo, já como tipos de saída: quem fala com o modelo chama este serviço e não toca nos
/// agregados. Os repositórios enxergam só o usuário atual. O mês vem do modelo e pode ser inválido: a falha volta no
/// envelope, com mensagem fixa, e não como exceção.
/// </summary>
public sealed class LeituraParaModeloService
{
    private readonly ITransacaoRepository _transacoes;
    private readonly IDespesaRecorrenteRepository _recorrentes;
    private readonly IDespesaRepository _despesas;

    public LeituraParaModeloService(
        ITransacaoRepository transacoes,
        IDespesaRecorrenteRepository recorrentes,
        IDespesaRepository despesas)
    {
        _transacoes = transacoes;
        _recorrentes = recorrentes;
        _despesas = despesas;
    }

    public async Task<TransacoesDoMesParaModelo> ListarTransacoesAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesReferencia = MesReferencia.Criar(mes, ano);
        if (mesReferencia.IsFailure)
            return TransacoesDoMesParaModelo.Falha(ErroParaModelo.MesInvalido);

        IReadOnlyList<Transacao> transacoes = await _transacoes.ListarDoMesAsync(mesReferencia.Value, cancellationToken);

        return TransacoesDoMesParaModelo.De(
            mesReferencia.Value,
            transacoes.Select(transacao => transacao.ParaModelo()).ToArray());
    }

    /// <summary>
    /// Toda despesa recorrente do usuário, com a despesa que ela gerou no mês, quando já gerou: um mês ainda não gerado
    /// mostra as recorrentes sem despesa, e não uma lista vazia.
    /// </summary>
    public async Task<DespesasFixasDoMesParaModelo> ListarDespesasFixasAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        Result<MesReferencia> mesReferencia = MesReferencia.Criar(mes, ano);
        if (mesReferencia.IsFailure)
            return DespesasFixasDoMesParaModelo.Falha(ErroParaModelo.MesInvalido);

        IReadOnlyList<DespesaRecorrente> recorrentes = await _recorrentes.ListarAsync(cancellationToken);
        IReadOnlyList<Despesa> despesasDoMes = await _despesas.ListarDoMesAsync(mesReferencia.Value, cancellationToken);

        // O índice único (id_despesa_recorrente, mes_referencia) garante no máximo uma despesa por recorrente no mês.
        Dictionary<IdDespesaRecorrente, Despesa> geradas = despesasDoMes
            .Where(despesa => despesa.IdDespesaRecorrente is not null)
            .ToDictionary(despesa => despesa.IdDespesaRecorrente!.Value);

        return DespesasFixasDoMesParaModelo.De(
            mesReferencia.Value,
            recorrentes
                .Select(recorrente => recorrente.ParaModelo(geradas.GetValueOrDefault(recorrente.Id), mesReferencia.Value))
                .ToArray());
    }
}
