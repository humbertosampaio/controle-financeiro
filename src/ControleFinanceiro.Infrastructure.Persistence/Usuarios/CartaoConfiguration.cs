using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Usuarios;

internal sealed class CartaoConfiguration : IEntityTypeConfiguration<Cartao>
{
    public void Configure(EntityTypeBuilder<Cartao> entity)
    {
        // Os CHECK repetem no esquema o formato que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("cartoes", tabela =>
        {
            tabela.HasCheckConstraint("CK_cartoes_banco", "banco ~ '^[0-9]{3}$'");
            tabela.HasCheckConstraint("CK_cartoes_sufixo", "sufixo ~ '^[0-9]{4}$'");
            tabela.HasCheckConstraint("CK_cartoes_dia_vencimento", "dia_vencimento BETWEEN 1 AND 28");
        });
        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .HasColumnName("id");

        entity.Property(c => c.Banco)
            .HasColumnName("banco");

        entity.Property(c => c.Sufixo)
            .HasColumnName("sufixo");

        entity.Property(c => c.DiaVencimento)
            .HasColumnName("dia_vencimento");

        entity.Property<IdUsuario>("id_usuario")
            .IsRequired();

        entity.HasIndex("id_usuario", nameof(Cartao.Banco), nameof(Cartao.Sufixo))
            .IsUnique();
    }
}
