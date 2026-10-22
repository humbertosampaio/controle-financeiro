using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entity)
    {
        entity.ToTable("usuarios");
        entity.HasKey(u => u.Id);

        entity.Property(u => u.Id)
            .HasColumnName("id");

        entity.Property(u => u.Nome)
            .HasColumnName("nome");

        entity.Property(u => u.RendaBruta)
            .HasColumnName("renda_bruta");

        entity.HasMany(u => u.DescontosSalariais)
            .WithOne()
            .HasForeignKey("id_usuario")
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(u => u.DescontosSalariais)
            .AutoInclude()
            .HasField("_descontosSalariais")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        entity.HasMany(u => u.Cartoes)
            .WithOne()
            .HasForeignKey(c => c.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(u => u.Cartoes)
            .AutoInclude()
            .HasField("_cartoes")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Mapeada na #6, junto da importação, que é quem cria as contas.
        entity.Ignore(u => u.Contas);
    }
}
