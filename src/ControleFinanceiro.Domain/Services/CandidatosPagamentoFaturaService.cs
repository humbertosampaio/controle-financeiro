using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Aponta as saídas da conta que provavelmente pagaram uma fatura, sem gravar nada: a fatura ainda sem pagamento, com
/// importação no mês e total positivo (<see cref="FaturaCartaoAberta"/>), e a saída livre, com o valor exatamente igual ao total
/// e a data na janela do <see cref="VencimentoFatura.AceitaPagamentoEm"/>. Só vira candidato o par único nos dois
/// sentidos: uma saída só para a fatura e uma fatura só para a saída, inclusive entre as faturas dos meses vizinhos, cujas
/// janelas cruzam as dos meses pedidos (<see cref="MesesConferidos"/>). O empate e a ausência ficam para a ligação manual.
/// Nada é alterado.
/// </summary>
public static class CandidatosPagamentoFaturaService
{
    /// <summary>
    /// Os meses de vencimento cujas faturas competem pelas saídas que podem pagar as dos <paramref name="mesesVencimento"/>,
    /// pelo <see cref="VencimentoFatura.MesesVencimentoFaturasConcorrentes"/> de cada mês, em ordem e sem repetir.
    /// Quem chama carrega as listas desses meses, e não só dos pedidos, para o empate entre meses vizinhos valer em
    /// qualquer porta de entrada.
    /// </summary>
    public static IReadOnlyList<MesReferencia> MesesConferidos(IReadOnlyCollection<MesReferencia> mesesVencimento)
    {
        return mesesVencimento
            .SelectMany(VencimentoFatura.MesesVencimentoFaturasConcorrentes)
            .Distinct()
            .Order()
            .ToList();
    }

    /// <param name="mesesVencimento">Os meses de vencimento das faturas cujos candidatos se quer.</param>
    /// <param name="cartoesUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <param name="importacoesFaturas">
    /// As importações de fatura com vencimento nos <see cref="MesesConferidos"/>, de onde sai o vencimento real.
    /// </param>
    /// <param name="transacoes">
    /// As transações das faturas dos <see cref="MesesConferidos"/>, que compõem os totais, e as saídas da conta que podem
    /// pagá-las. As demais são ignoradas.
    /// </param>
    /// <param name="despesasMeses">As despesas com pagamento nos <see cref="MesesConferidos"/>, para saber que fatura já foi paga.</param>
    /// <param name="despesasPagasSaidas">As despesas que alguma das saídas já pagou, inteiras ou por uma das parcelas.</param>
    /// <returns>Os candidatos das faturas dos meses pedidos, um por fatura e por saída, na ordem dos meses e das faturas.</returns>
    /// <exception cref="ArgumentException">Quando um item das listas é de outro usuário: é bug de quem chama.</exception>
    public static IReadOnlyList<CandidatoPagamentoFatura> Apontar(
        IdUsuario idUsuario,
        IReadOnlyCollection<MesReferencia> mesesVencimento,
        IReadOnlyCollection<Cartao> cartoesUsuario,
        IReadOnlyCollection<ImportacaoFatura> importacoesFaturas,
        IReadOnlyCollection<Transacao> transacoes,
        IReadOnlyCollection<Despesa> despesasMeses,
        IReadOnlyCollection<Despesa> despesasPagasSaidas)
    {
        cartoesUsuario.ConferirDono(idUsuario, nameof(cartoesUsuario));
        importacoesFaturas.ConferirDono(idUsuario, nameof(importacoesFaturas));
        transacoes.ConferirDono(idUsuario, nameof(transacoes));
        despesasMeses.ConferirDono(idUsuario, nameof(despesasMeses));
        despesasPagasSaidas.ConferirDono(idUsuario, nameof(despesasPagasSaidas));

        FaturaCartaoAberta[] faturas = MesesConferidos(mesesVencimento)
            .SelectMany(mes => FaturaCartao.TodasVencendoEm(cartoesUsuario, mes))
            .Where(fatura => !EstaPaga(fatura, despesasMeses))
            .Select(fatura => Abrir(fatura, importacoesFaturas, transacoes))
            .Choose()
            .ToArray();

        return Emparelhar(faturas, SaidasLivres(transacoes, despesasPagasSaidas))
            .Where(candidato => mesesVencimento.Contains(candidato.FaturaCartaoAberta.Fatura.Vencimento.Mes))
            .ToList();
    }

    private static bool EstaPaga(FaturaCartao fatura, IReadOnlyCollection<Despesa> despesasMeses) =>
        despesasMeses.OfType<IDespesaDoCartao>().Any(despesa => despesa.EstaPagaPelaFatura(fatura));

    /// <summary>
    /// A fatura aberta, quando alguma importação a trouxe e o total é positivo. Todas as importações da fatura no mês têm
    /// o mesmo vencimento, pela <see cref="ImportacaoArquivoFactory.ConferirVencimentoFatura"/>, então qualquer uma serve.
    /// </summary>
    private static Maybe<FaturaCartaoAberta> Abrir(
        FaturaCartao fatura,
        IReadOnlyCollection<ImportacaoFatura> importacoesFaturas,
        IReadOnlyCollection<Transacao> transacoes)
    {
        return importacoesFaturas
            .Select(importacao => importacao.VencimentoImportado(fatura))
            .Choose()
            .TryFirst()
            .Bind(vencimento => FaturaCartaoAberta.Abrir(fatura, vencimento, transacoes));
    }

    /// <summary>As saídas da conta que ainda não pagaram nenhuma despesa.</summary>
    private static Transacao[] SaidasLivres(IReadOnlyCollection<Transacao> transacoes, IReadOnlyCollection<Despesa> despesasPagasSaidas)
    {
        return transacoes
            .Where(transacao => transacao.EhSaidaConta)
            .Where(saida => despesasPagasSaidas.All(despesa => despesa.PagamentosRealizadosPor(saida.Id).Count == 0))
            .DistinctBy(saida => saida.Id)
            .ToArray();
    }

    /// <summary>Os pares em que a fatura aceita só aquela saída e a saída só cabe naquela fatura.</summary>
    private static CandidatoPagamentoFatura[] Emparelhar(FaturaCartaoAberta[] faturas, Transacao[] saidas)
    {
        (FaturaCartaoAberta Fatura, Transacao Saida)[] pares = faturas
            .SelectMany(fatura => saidas
                .Where(saida => fatura.AceitaPagamento(saida.ValorAbsoluto, saida.Data))
                .Select(saida => (Fatura: fatura, Saida: saida)))
            .ToArray();

        return pares
            .Where(par => pares.Count(outro => outro.Fatura == par.Fatura) == 1)
            .Where(par => pares.Count(outro => outro.Saida.Id == par.Saida.Id) == 1)
            .Select(par => new CandidatoPagamentoFatura(par.Saida.Id, par.Fatura))
            .ToArray();
    }
}
