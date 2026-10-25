namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarVencimentoCartao;

public sealed record class AlterarVencimentoCartaoRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdCartao { get; init; }

    public int DiaVencimento { get; init; }
}
