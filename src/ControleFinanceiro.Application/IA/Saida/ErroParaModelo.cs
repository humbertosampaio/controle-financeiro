using System.Text.Json.Serialization;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// Uma falha prevista, com mensagem fixa que diz ao modelo o que corrigir. Nunca repete a entrada nem o erro de quem
/// falhou.
/// </summary>
public sealed record ErroParaModelo : ISaidaModelo
{
    public static readonly ErroParaModelo MesInvalido = Fixo("Mês inválido: o mês vai de 1 a 12, e o ano, de 1 a 9999.");

    public static readonly ErroParaModelo UsuarioNaoEncontrado = Fixo("O usuário configurado no servidor não foi encontrado.");

    public static readonly ErroParaModelo TransacaoNaoEncontrada = Fixo("A transação não foi encontrada: use o id de uma transação de listar_transacoes.");

    public static readonly ErroParaModelo DespesaNaoEncontrada = Fixo("A despesa não foi encontrada: use o id de uma despesa de listar_despesas_fixas.");

    public static readonly ErroParaModelo TransacaoJaVinculada = Fixo("A transação já paga ou cobra uma despesa, e cada transação é ligada a uma só.");

    public static readonly ErroParaModelo DespesaJaPaga = Fixo("A despesa já foi paga por outra saída.");

    public static readonly ErroParaModelo DespesaJaCobrada = Fixo("A despesa já foi cobrada por outra compra do cartão.");

    public static readonly ErroParaModelo TransacaoNaoEhSaida = Fixo("A transação é uma entrada: só uma saída paga despesa.");

    public static readonly ErroParaModelo CartaoNaoEncontrado = Fixo("O cartão não foi encontrado: use o id de um cartão de listar_cartoes.");

    public static readonly ErroParaModelo OrigemIncompativel = Fixo("A origem da transação não combina com a despesa: a despesa sem cartão é paga por uma saída da conta, a do crédito é cobrada por uma compra do cartão dela, e a fatura do cartão é paga por uma saída da conta.");

    public static readonly ErroParaModelo ParcelaIncompativel = Fixo("A parcela da transação não combina com a despesa: só a despesa parcelada é cobrada por parcela, e só pela parcela da mesma compra.");

    public static readonly ErroParaModelo TransacaoPagaOutraFatura = Fixo("A saída já pagou a fatura de outro cartão ou de outro mês de vencimento, e cada saída paga uma fatura só.");

    public static readonly ErroParaModelo FaturaSemCobrancaPendente = Fixo("A fatura não tem cobrança pendente nesse mês de vencimento: confira o cartão e o mês em que a fatura vence, e se a fatura já foi importada.");

    public static readonly ErroParaModelo ResumoDivergente = Fixo("O resumo não corresponde ao vínculo desses ids, e nada foi gravado: chame prever_vinculo de novo, mostre o novo resumo ao usuário e confirme com ele sem alteração.");

    public static readonly ErroParaModelo ResumoPagamentoFaturaDivergente = Fixo("O resumo não corresponde ao pagamento da fatura com essa saída, esse cartão e esse mês, e nada foi gravado: chame prever_pagamento_fatura de novo, mostre o novo resumo ao usuário e confirme com ele sem alteração.");

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; }

    private ErroParaModelo(DocumentoMascarado mensagem)
    {
        Mensagem = mensagem;
    }

    /// <summary>Para mensagem escrita aqui, sem dado do usuário: ela passa pela máscara como qualquer texto.</summary>
    private static ErroParaModelo Fixo(string mensagem) => new((DocumentoMascarado)mensagem);

    /// <summary>
    /// A mensagem fixa de um erro do vínculo ou do pagamento da fatura, sem a descrição do domínio nem a despesa que já usa
    /// a transação.
    /// </summary>
    /// <exception cref="InvalidOperationException">Quando o erro não tem mensagem mapeada.</exception>
    public static ErroParaModelo De(Erro erro)
    {
        return erro switch
        {
            ErroTransacaoJaVinculada => TransacaoJaVinculada,
            ErroDespesaJaPaga => DespesaJaPaga,
            ErroDespesaJaCobrada => DespesaJaCobrada,
            ErroTransacaoNaoEhSaida => TransacaoNaoEhSaida,
            ErroOrigemIncompativelComDespesa => OrigemIncompativel,
            ErroParcelaIncompativelComDespesa => ParcelaIncompativel,
            ErroTransacaoPagaOutraFatura => TransacaoPagaOutraFatura,
            ErroFaturaSemCobrancaPendente => FaturaSemCobrancaPendente,
            _ => throw new InvalidOperationException($"Erro sem mensagem mapeada: {erro}"),
        };
    }
}
