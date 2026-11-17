using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoContaConfiguration : IEntityTypeConfiguration<TransacaoConta>
{
    public void Configure(EntityTypeBuilder<TransacaoConta> entity)
    {
        entity.Property(t => t.IdConta)
            .HasColumnName("id_conta");

        entity.HasOne<ContaBancaria>()
            .WithMany()
            .HasForeignKey(t => t.IdConta)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(t => new { t.IdConta, t.Data });
    }
}
