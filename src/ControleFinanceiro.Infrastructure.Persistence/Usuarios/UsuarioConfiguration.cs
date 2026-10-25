using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("usuarios", tabela =>
        {
            tabela.HasCheckConstraint("CK_usuarios_nome", "btrim(nome) <> ''");
            tabela.HasCheckConstraint("CK_usuarios_renda_bruta", "renda_bruta >= 0");
        });
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
    }
}
