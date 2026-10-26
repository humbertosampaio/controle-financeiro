using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Categorias;

internal sealed class CategoriaDespesaConfiguration : IEntityTypeConfiguration<CategoriaDespesa>
{
    public void Configure(EntityTypeBuilder<CategoriaDespesa> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("categorias_despesa", tabela =>
            tabela.HasCheckConstraint("CK_categorias_despesa_nome", "btrim(nome) <> ''"));
        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .HasColumnName("id");

        entity.Property(c => c.IdUsuario)
            .HasColumnName("id_usuario");

        entity.Property(c => c.Nome)
            .HasColumnName("nome");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.OwnsMany(c => c.Aliases, aliases =>
        {
            aliases.ToTable("aliases_categorias", tabela =>
                tabela.HasCheckConstraint("CK_aliases_categorias_texto_normalizado", "btrim(texto_normalizado) <> ''"));

            aliases.WithOwner()
                .HasForeignKey("id_categoria");

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

        entity.Navigation(c => c.Aliases)
            .HasField("_aliases")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
