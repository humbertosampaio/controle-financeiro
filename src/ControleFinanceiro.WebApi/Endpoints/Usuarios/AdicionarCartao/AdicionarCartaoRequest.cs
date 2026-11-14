namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

public sealed record class AdicionarCartaoRequest
{
    /// <summary>Vem da rota, que vence o corpo.</summary>
    public Guid IdUsuario { get; init; }

    /// <summary>Código COMPE do banco emissor, com os 3 dígitos, como o do <see cref="Domain.Shared.ValueObjects.Banco.C6"/>.</summary>
    public string Banco { get; init; } = string.Empty;

    public string? Sufixo { get; init; }
    public int DiaVencimento { get; init; }
    public int DiaFechamento { get; init; }
}
