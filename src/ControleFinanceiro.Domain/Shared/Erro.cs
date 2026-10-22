namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// Uma falha prevista do domínio que é uma situação de negócio com nome próprio. Cada uma é um tipo
/// derivado, na pasta do seu agregado, e quem chama decide o que fazer pelo tipo. O <see cref="Codigo"/> é
/// o identificador estável da falha, para log, para a resposta da API e para o MCP. A
/// <see cref="Descricao"/> explica a falha a quem usa a API, não entra na igualdade e pode citar o banco e
/// o final de uma conta ou de um cartão, mas nunca outro conteúdo do arquivo importado. Ao modelo vão só o
/// <see cref="Codigo"/> e os dados tipados do erro, nunca a <see cref="Descricao"/>.
/// </summary>
public abstract class Erro : ComparableValueObject
{
    public string Codigo { get; }
    public string Descricao { get; }

    protected Erro(string codigo, string descricao)
    {
        Codigo = codigo;
        Descricao = descricao;
    }

    public override string ToString() => $"{Codigo}: {Descricao}";

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Codigo;
    }
}
