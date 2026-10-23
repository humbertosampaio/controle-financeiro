using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Contas;

internal sealed class ContaBancariaConfiguration : IEntityTypeConfiguration<ContaBancaria>
{
    public void Configure(EntityTypeBuilder<ContaBancaria> entity)
    {
        // Os CHECK repetem no esquema o formato que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("contas", tabela =>
        {
            tabela.HasCheckConstraint("CK_contas_banco", "banco ~ '^[0-9]{3}$'");
            tabela.HasCheckConstraint("CK_contas_sufixo", "sufixo ~ '^[0-9]{3}[0-9X]$'");
        });

        entity.HasKey(c => c.Id);

        entity.Property(c => c.Id)
            .HasColumnName("id");

        entity.Property(c => c.Banco)
            .HasColumnName("banco");

        entity.Property(c => c.Tipo)
            .HasColumnName("tipo");

        entity.Property(c => c.Sufixo)
            .HasColumnName("sufixo")
            .HasConversion(sufixo => sufixo.Value, valor => (SufixoConta)valor)
            .HasMaxLength(SufixoConta.QuantidadeCaracteres);

        entity.Property(c => c.Apelido)
            .HasColumnName("apelido");

        entity.Property(c => c.IdUsuario)
            .HasColumnName("id_usuario");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(c => c.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(c => new { c.IdUsuario, c.Banco, c.Sufixo })
            .IsUnique();
    }
}
