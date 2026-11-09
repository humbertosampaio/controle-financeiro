namespace ControleFinanceiro.WebApi.Endpoints.Despesas.DesfazerPagamentoFatura;

/// <summary>Todos os campos vêm da rota.</summary>
public sealed record class DesfazerPagamentoFaturaRequest
{
    public Guid IdUsuario { get; init; }

    public Guid IdTransacao { get; init; }
}
