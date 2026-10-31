using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaParceladaConfiguration : IEntityTypeConfiguration<DespesaParcelada>
{
    public void Configure(EntityTypeBuilder<DespesaParcelada> entity)
    {
        // A coluna, a FK e o índice são os mesmos da DespesaAdiada: o EF os funde por estarem na mesma tabela.
        entity.Property(d => d.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(d => d.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasMany(d => d.Parcelas)
            .WithOne()
            .HasForeignKey("id_despesa")
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(d => d.Parcelas)
            .AutoInclude()
            .HasField("_parcelas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
