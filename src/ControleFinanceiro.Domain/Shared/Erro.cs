namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// Uma falha previsível de uma operação do domínio. O <see cref="Codigo"/> identifica a falha para quem
/// chama decidir o que fazer; a <see cref="Descricao"/> explica a falha e não traz conteúdo do arquivo
/// importado. As instâncias saem de <see cref="Erros"/>.
/// </summary>
public sealed class Erro : ValueObject
{
    public string Codigo { get; }
    public string Descricao { get; }

    internal Erro(string codigo, string descricao)
    {
        Codigo = codigo;
        Descricao = descricao;
    }

    public override string ToString() => $"{Codigo}: {Descricao}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Codigo;
        yield return Descricao;
    }
}
