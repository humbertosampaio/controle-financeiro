using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Cartoes;

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
            .HasColumnName("sufixo")
            .HasConversion(sufixo => sufixo.Value, valor => (SufixoCartao)valor)
            .HasMaxLength(SufixoCartao.QuantidadeDigitos);

        entity.Property(c => c.DiaVencimento)
            .HasColumnName("dia_vencimento")
            .HasConversion(dia => dia.Value, valor => (DiaVencimentoCartao)valor);

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
