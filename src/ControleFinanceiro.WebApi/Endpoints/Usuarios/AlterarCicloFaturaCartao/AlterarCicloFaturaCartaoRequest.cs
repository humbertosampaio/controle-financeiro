namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;

public sealed record class AlterarCicloFaturaCartaoRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdCartao { get; init; }

    public int DiaVencimento { get; init; }
    public int DiaFechamento { get; init; }
}
