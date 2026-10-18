using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Categorias;

[ValueObject]
public readonly partial struct IdCategoriaDespesa
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class CategoriaDespesa : AggregateRoot<IdCategoriaDespesa>
{
    public long IdUsuario { get; private set; }
    public TextoObrigatorio Nome { get; private set; }

    private CategoriaDespesa(IdCategoriaDespesa id, long idUsuario, TextoObrigatorio nome)
        : base(id)
    {
        IdUsuario = idUsuario;
        Nome = nome;
    }

    public static CategoriaDespesa Criar(long idUsuario, TextoObrigatorio nome) =>
        new(IdCategoriaDespesa.FromNewVersion7Guid(), idUsuario, nome);

    public void Renomear(TextoObrigatorio novoNome) => Nome = novoNome;
}
