namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesvincularTransacao;

/// <summary>Todos os campos vêm da rota.</summary>
public sealed record class DesvincularTransacaoRequest
{
    public Guid IdUsuario { get; init; }

    public Guid IdDespesa { get; init; }

    public Guid IdTransacao { get; init; }
}
