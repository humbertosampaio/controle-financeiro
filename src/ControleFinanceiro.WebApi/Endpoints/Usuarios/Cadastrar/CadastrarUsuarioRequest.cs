namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;

public sealed record class CadastrarUsuarioRequest
{
    public string Nome { get; init; } = string.Empty;
    public decimal RendaBruta { get; init; }
    public IEnumerable<DescontoSalarialDto> DescontosSalariais { get; init; } = [];
    public CartaoDto Cartao { get; init; } = null!;
}

public sealed record class CartaoDto
{
    public string? Sufixo { get; init; }
    public int DiaVencimento { get; init; }
}

public sealed record class DescontoSalarialDto
{
    public string? Descricao { get; init; }
    public decimal Valor { get; init; }
}