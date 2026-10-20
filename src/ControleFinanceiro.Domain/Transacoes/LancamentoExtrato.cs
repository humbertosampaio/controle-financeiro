using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Transacoes;

/// <summary>
/// Uma linha do extrato ou da fatura, como o adaptador do layout a leu, antes de virar transação.
/// </summary>
public sealed class LancamentoExtrato
{
    public DateOnly Data { get; }

    /// <summary>Preenchida quando o layout traz a hora.</summary>
    public TimeOnly? Hora { get; }

    /// <summary>Com sinal: negativo é saída. Zero é aceito.</summary>
    public Reais Valor { get; }

    public TextoObrigatorio Descricao { get; }

    /// <summary>Aparada; nula quando o layout não traz ou traz em branco.</summary>
    public string? Contraparte { get; }

    /// <summary>Os campos que o layout declara para reconhecer o lançamento numa reimportação.</summary>
    public IdentidadeLancamento Identidade { get; }

    private LancamentoExtrato(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        IdentidadeLancamento identidade)
    {
        Data = data;
        Hora = hora;
        Valor = valor;
        Descricao = descricao;
        Contraparte = contraparte;
        Identidade = identidade;
    }

    public static LancamentoExtrato Criar(
        DateOnly data,
        TimeOnly? hora,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        IdentidadeLancamento identidade)
    {
        string? contraparteAparada = string.IsNullOrWhiteSpace(contraparte) ? null : contraparte.Trim();
        return new LancamentoExtrato(data, hora, valor, descricao, contraparteAparada, identidade);
    }
}
