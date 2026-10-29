using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaAdiadaConfiguration : IEntityTypeConfiguration<DespesaAdiada>
{
    public void Configure(EntityTypeBuilder<DespesaAdiada> entity)
    {
        entity.Property(d => d.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(d => d.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
