using ControleFinanceiro.Application.SaidaModelo;
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

    public LeituraParaModeloService(ITransacaoRepository transacoes)
    {
        _transacoes = transacoes;
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
}
