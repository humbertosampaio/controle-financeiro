using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>O que a importação da fatura faz num cartão: só o final e as contagens, sem os lançamentos.</summary>
public sealed record CartaoImportadoParaModelo : ISaidaModelo
{
    [JsonPropertyName("final")]
    public string Final { get; }

    [JsonPropertyName("lidas")]
    public int Lidas { get; }

    [JsonPropertyName("novas")]
    public int Novas { get; }

    [JsonPropertyName("ja_existentes")]
    public int JaExistentes { get; }

    /// <summary>As despesas parceladas que a importação cria para as compras parceladas novas.</summary>
    [JsonPropertyName("despesas_parceladas")]
    public int DespesasParceladas { get; }

    /// <summary>As despesas adiadas, já cobradas, que a importação cria para as compras à vista novas sem despesa prevista.</summary>
    [JsonPropertyName("despesas_adiadas")]
    public int DespesasAdiadas { get; }

    private CartaoImportadoParaModelo(
        SufixoCartao final,
        int lidas,
        int novas,
        int jaExistentes,
        int despesasParceladas,
        int despesasAdiadas)
    {
        Final = final;
        Lidas = lidas;
        Novas = novas;
        JaExistentes = jaExistentes;
        DespesasParceladas = despesasParceladas;
        DespesasAdiadas = despesasAdiadas;
    }

    public static CartaoImportadoParaModelo De(FaturaImportada.CartaoImportado cartao)
    {
        return new CartaoImportadoParaModelo(
            cartao.Sufixo,
            cartao.Lidas,
            cartao.Novas,
            cartao.JaExistentes,
            cartao.DespesasParceladasCriadas,
            cartao.DespesasAdiadasCriadas);
    }
}
