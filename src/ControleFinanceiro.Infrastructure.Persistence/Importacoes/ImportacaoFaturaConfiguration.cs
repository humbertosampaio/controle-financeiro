using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoFaturaConfiguration : IEntityTypeConfiguration<ImportacaoFatura>
{
    public void Configure(EntityTypeBuilder<ImportacaoFatura> entity)
    {
        entity.Property(i => i.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(i => i.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(i => i.Vencimento)
            .HasColumnName("data_vencimento_fatura")
            .HasConversion(vencimento => vencimento.Value, data => (VencimentoFatura)data);
    }
}
