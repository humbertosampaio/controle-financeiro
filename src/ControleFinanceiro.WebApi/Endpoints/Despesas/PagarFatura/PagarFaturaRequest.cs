namespace ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;

public sealed record class PagarFaturaRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>A saída da conta que pagou a fatura.</summary>
    public Guid IdTransacao { get; init; }

    /// <summary>Um dos cartões da fatura; os outros finais da mesma fatura entram junto.</summary>
    public Guid IdCartao { get; init; }

    /// <summary>O ano do vencimento da fatura.</summary>
    public int Ano { get; init; }

    /// <summary>O mês do vencimento da fatura.</summary>
    public int Mes { get; init; }
}
