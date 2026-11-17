using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoExtratoConfiguration : IEntityTypeConfiguration<ImportacaoExtrato>
{
    public void Configure(EntityTypeBuilder<ImportacaoExtrato> entity)
    {
        entity.Property(i => i.IdConta)
            .HasColumnName("id_conta");

        entity.HasOne<ContaBancaria>()
            .WithMany()
            .HasForeignKey(i => i.IdConta)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
