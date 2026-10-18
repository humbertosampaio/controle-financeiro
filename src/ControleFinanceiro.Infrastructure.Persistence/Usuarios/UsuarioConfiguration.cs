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
            .HasField("_descontosSalariais")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        entity.OwnsOne(u => u.Cartao, cartaoBuilder =>
        {
            cartaoBuilder.Property(c => c.Sufixo)
                .HasColumnName("cartao_sufixo");

            cartaoBuilder.Property(c => c.DiaVencimento)
                .HasColumnName("cartao_dia_vencimento");
        });

        entity.Ignore(u => u.DespesasRecorrentes);
    }
}
