using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Tests.Shared;

public sealed class AggregateRootTests
{
    [Fact]
    public void LimparEventos_Deve_EsvaziarEventos()
    {
        IAggregateRoot raiz = new AggregateRootStub(new DomainEventStub());

        raiz.LimparEventos();

        raiz.Eventos.Should().BeEmpty();
    }

    private sealed record DomainEventStub : IDomainEvent;

    private sealed class AggregateRootStub : AggregateRoot<Guid>
    {
        public AggregateRootStub(IDomainEvent evento)
        {
            RegistrarEvento(evento);
        }
    }
}
