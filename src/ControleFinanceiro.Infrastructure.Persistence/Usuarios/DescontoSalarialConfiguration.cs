using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class DescontoSalarialConfiguration : IEntityTypeConfiguration<DescontoSalarial>
{
    public void Configure(EntityTypeBuilder<DescontoSalarial> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("descontos_salariais", tabela =>
        {
            tabela.HasCheckConstraint("CK_descontos_salariais_descricao", "btrim(descricao) <> ''");
            tabela.HasCheckConstraint("CK_descontos_salariais_valor", "valor >= 0");
        });
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
