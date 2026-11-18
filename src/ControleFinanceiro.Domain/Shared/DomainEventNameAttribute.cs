namespace ControleFinanceiro.Domain.Shared;

/// <summary>
/// O nome estável com que o evento é gravado, independente do nome da classe: renomear o evento não pode deixar sem tipo
/// as mensagens já gravadas com o nome antigo. Todo <see cref="IDomainEvent"/> declara o seu, e nunca o troca.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DomainEventNameAttribute : Attribute
{
    public string Name { get; }

    public DomainEventNameAttribute(string name)
    {
        Name = name;
    }
}
