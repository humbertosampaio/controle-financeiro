namespace ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;

public sealed record class VincularTransacaoRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdDespesa { get; init; }

    public Guid IdTransacao { get; init; }
}
