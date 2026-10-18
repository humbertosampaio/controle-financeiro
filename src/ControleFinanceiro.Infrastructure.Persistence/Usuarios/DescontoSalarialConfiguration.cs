using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class DescontoSalarialConfiguration : IEntityTypeConfiguration<DescontoSalarial>
{
    public void Configure(EntityTypeBuilder<DescontoSalarial> entity)
    {
        entity.ToTable("descontos_salariais");
        entity.HasKey(d => d.Id);

        entity.Property(d => d.Id)
            .HasColumnName("id");

        entity.Property(d => d.Descricao)
            .HasColumnName("descricao");

        entity.Property(d => d.Valor)
            .HasColumnName("valor");

        entity.Property<IdUsuario>("id_usuario")
            .IsRequired();
    }
}
