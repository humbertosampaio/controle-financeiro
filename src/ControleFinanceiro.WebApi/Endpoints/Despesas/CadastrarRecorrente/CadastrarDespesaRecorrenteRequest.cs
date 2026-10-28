namespace ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;

public sealed record class CadastrarDespesaRecorrenteRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    public Guid IdBeneficiario { get; init; }
    public string? Descricao { get; init; }
    public Guid IdCategoria { get; init; }

    /// <summary>O código da forma de pagamento: 1 (Pix), 2 (crédito) ou 3 (boleto).</summary>
    public int FormaPagamento { get; init; }

    /// <summary>Obrigatório no crédito e proibido nas outras formas.</summary>
    public Guid? IdCartao { get; init; }

    public decimal Valor { get; init; }
    public int DiaOcorrencia { get; init; }
}
