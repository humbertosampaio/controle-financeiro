using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>Os cartões do usuário, para o modelo achar o id do cartão de uma fatura.</summary>
public sealed record CartoesParaModelo : ISaidaModelo
{
    [JsonPropertyName("cartoes")]
    public IReadOnlyList<CartaoParaModelo> Cartoes { get; }

    private CartoesParaModelo(IReadOnlyList<CartaoParaModelo> cartoes)
    {
        Cartoes = cartoes;
    }

    public static CartoesParaModelo De(IReadOnlyList<CartaoParaModelo> cartoes) => new(cartoes);
}
