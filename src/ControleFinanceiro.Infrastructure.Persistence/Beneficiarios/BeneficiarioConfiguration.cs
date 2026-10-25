using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Beneficiarios;

internal sealed class BeneficiarioConfiguration : IEntityTypeConfiguration<Beneficiario>
{
    public void Configure(EntityTypeBuilder<Beneficiario> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("beneficiarios", tabela =>
            tabela.HasCheckConstraint("CK_beneficiarios_nome", "btrim(nome) <> ''"));
        entity.HasKey(b => b.Id);

        entity.Property(b => b.Id)
            .HasColumnName("id");

        entity.Property(b => b.IdUsuario)
            .HasColumnName("id_usuario");

        entity.Property(b => b.Nome)
            .HasColumnName("nome");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(b => b.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.OwnsMany(b => b.Aliases, aliases =>
        {
            aliases.ToTable("aliases_beneficiarios", tabela =>
                tabela.HasCheckConstraint("CK_aliases_beneficiarios_texto_normalizado", "btrim(texto_normalizado) <> ''"));

            aliases.WithOwner()
                .HasForeignKey("id_beneficiario");

            aliases.Property<int>("id");
            aliases.HasKey("id");

            aliases.Property(a => a.IdUsuario)
                .HasColumnName("id_usuario");

            // Owned dentro do owned, na mesma tabela: o EF não aceita complex type num owned. Um índice não junta
            // colunas de dois tipos, então o índice único (id_usuario, texto_normalizado) vai por SQL na migration.
            aliases.OwnsOne(a => a.Alias, alias =>
            {
                alias.Property(a => a.TextoOriginal)
                    .HasColumnName("texto_original");

                alias.Property(a => a.TextoNormalizado)
                    .HasColumnName("texto_normalizado");
            });
        });

        entity.Navigation(b => b.Aliases)
            .HasField("_aliases")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
