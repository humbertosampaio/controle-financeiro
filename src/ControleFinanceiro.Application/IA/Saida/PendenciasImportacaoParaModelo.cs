using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// O que o usuário precisa resolver pela API antes de importar o arquivo. Os beneficiários e as categorias vão só como
/// quantidade, porque os textos são do arquivo; os finais de cartão vão inteiros, como em <c>listar_cartoes</c>.
/// </summary>
public sealed record PendenciasImportacaoParaModelo : ISaidaModelo
{
    /// <summary>Os textos de beneficiário do arquivo que ainda não são alias de nenhum beneficiário.</summary>
    [JsonPropertyName("beneficiarios")]
    public int Beneficiarios { get; }

    /// <summary>As categorias do banco no arquivo que ainda não são alias de nenhuma categoria.</summary>
    [JsonPropertyName("categorias")]
    public int Categorias { get; }

    /// <summary>Os finais de cartão da fatura sem cartão cadastrado no banco dela.</summary>
    [JsonPropertyName("finais_sem_cartao")]
    public IReadOnlyList<string> FinaisSemCartao { get; }

    private PendenciasImportacaoParaModelo(int beneficiarios, int categorias, IReadOnlyList<SufixoCartao> finaisSemCartao)
    {
        Beneficiarios = beneficiarios;
        Categorias = categorias;
        FinaisSemCartao = finaisSemCartao.Select(final => (string)final).ToArray();
    }

    /// <param name="beneficiarios">Os aliases de beneficiário pendentes do extrato.</param>
    public static PendenciasImportacaoParaModelo DoExtrato(IReadOnlyList<Alias> beneficiarios) =>
        new(beneficiarios.Count, categorias: 0, finaisSemCartao: []);

    public static PendenciasImportacaoParaModelo DaFatura(PendenciasFatura pendencias)
    {
        return new PendenciasImportacaoParaModelo(
            pendencias.Beneficiarios.Count,
            pendencias.CategoriasBanco.Count,
            pendencias.Finais.Where(final => final.Cartao.HasNoValue).Select(final => final.Sufixo).ToArray());
    }
}
