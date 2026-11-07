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

    public static readonly ErroParaModelo TransacaoJaVinculada = Fixo("A transação já paga uma despesa, e cada transação paga uma só.");

    public static readonly ErroParaModelo DespesaJaPaga = Fixo("A despesa já foi quitada por outra transação.");

    public static readonly ErroParaModelo TransacaoNaoEhSaida = Fixo("A transação é uma entrada: só uma saída paga despesa.");

    public static readonly ErroParaModelo OrigemIncompativel = Fixo("A origem da transação não combina com a despesa: a despesa sem cartão é paga por uma saída da conta, e a do crédito, por uma saída do cartão dela.");

    public static readonly ErroParaModelo ParcelaIncompativel = Fixo("A parcela da transação não combina com a despesa: só a despesa parcelada é paga por parcela, e só pela parcela da mesma compra.");

    public static readonly ErroParaModelo ConfirmacaoRecusada = Fixo("O usuário não confirmou o vínculo, e nada foi gravado.");

    public static readonly ErroParaModelo ConfirmacaoIndisponivel = Fixo("Não foi possível pedir a confirmação do usuário, e nada foi gravado: o cliente MCP precisa aceitar elicitation.");

    [JsonPropertyName("mensagem")]
    public string Mensagem { get; }

    private ErroParaModelo(DocumentoMascarado mensagem)
    {
        Mensagem = mensagem;
    }

    /// <summary>Para mensagem escrita aqui, sem dado do usuário: ela passa pela máscara como qualquer texto.</summary>
    private static ErroParaModelo Fixo(string mensagem) => new((DocumentoMascarado)mensagem);

    /// <summary>A mensagem fixa de um erro do vínculo, sem a descrição do domínio nem a despesa que já usa a transação.</summary>
    /// <exception cref="InvalidOperationException">Quando o erro não tem mensagem mapeada.</exception>
    public static ErroParaModelo De(Erro erro)
    {
        return erro switch
        {
            ErroTransacaoJaVinculada => TransacaoJaVinculada,
            ErroDespesaJaPaga => DespesaJaPaga,
            ErroTransacaoNaoEhSaida => TransacaoNaoEhSaida,
            ErroOrigemIncompativelComDespesa => OrigemIncompativel,
            ErroParcelaIncompativelComDespesa => ParcelaIncompativel,
            _ => throw new InvalidOperationException($"Erro sem mensagem mapeada: {erro}"),
        };
    }
}
