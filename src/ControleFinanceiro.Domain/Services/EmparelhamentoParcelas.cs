using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Decide qual transação paga qual parcela dentro de uma mesma <see cref="ParcelaDaCompra"/>. A chave não inclui o valor:
/// sem a rodada por valor igual, gêmeas de valores diferentes poderiam trocar de despesa entre meses e registrar o efetivo
/// errado. O que sobra casa na ordem de valor. O desempate pelo id torna o resultado independente da ordem das listas.
/// </summary>
internal static class EmparelhamentoParcelas
{
    public static IEnumerable<Vinculo> Emparelhar(IEnumerable<Transacao> transacoes, IEnumerable<Vaga> vagas)
    {
        List<Vaga> vagasLivres = vagas.OrderBy(v => v.Despesa.Id).ToList();
        List<Transacao> semValorIgual = [];
        List<Vinculo> vinculos = [];

        foreach (Transacao transacao in transacoes.OrderBy(t => t.Id))
        {
            ReaisPositivo valor = transacao.ValorAbsoluto;
            Vaga? vaga = vagasLivres.Find(v => v.ValorProjetado == valor);
            if (vaga is null)
            {
                semValorIgual.Add(transacao);
                continue;
            }

            vagasLivres.Remove(vaga);
            vinculos.Add(new Vinculo(transacao, vaga.Despesa));
        }

        IEnumerable<Vinculo> naOrdemDeValor = semValorIgual
            .OrderBy(t => t.ValorAbsoluto)
            .ThenBy(t => t.Id)
            .Zip(
                vagasLivres.OrderBy(v => v.ValorProjetado).ThenBy(v => v.Despesa.Id),
                (transacao, vaga) => new Vinculo(transacao, vaga.Despesa));

        return vinculos.Concat(naOrdemDeValor);
    }

    public sealed record Vaga(DespesaParcelada Despesa, ParcelaDespesa Parcela)
    {
        public ReaisPositivo ValorProjetado => Parcela.ValorProjetado;
    }

    public sealed record Vinculo(Transacao Transacao, DespesaParcelada Despesa);
}
