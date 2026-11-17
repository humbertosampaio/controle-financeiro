using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>Uma linha do extrato de uma conta, que nunca traz parcela nem a marca de pagamento da fatura.</summary>
public sealed class LancamentoConta : Lancamento
{
    private LancamentoConta(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
            : base(data, hora, valor, descricao, contraparte, categoriaBanco, identidade)
    {
    }

    public static LancamentoConta Criar(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return new LancamentoConta(data, hora, valor, descricao, contraparte, categoriaBanco, identidade);
    }

    /// <summary>A contraparte na saída, nula em boleto e tarifa.</summary>
    public override string? TextoBeneficiario => TextoBeneficiarioDe(EhSaida, Contraparte);

    /// <summary>A regra do <see cref="TextoBeneficiario"/>, também para a transação, que não guarda o lançamento.</summary>
    internal static string? TextoBeneficiarioDe(bool ehSaida, string? contraparte) => ehSaida ? contraparte : null;

    protected override IEnumerable<object?> ComponentesIgualdadeOrigem() => [];
}
