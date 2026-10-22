using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

internal sealed class ImportacaoExtratoConfiguration : IEntityTypeConfiguration<ImportacaoExtrato>
{
    public void Configure(EntityTypeBuilder<ImportacaoExtrato> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("importacoes", tabela =>
        {
            tabela.HasCheckConstraint("CK_importacoes_origem", "num_nonnulls(id_conta, id_cartao) = 1");
            tabela.HasCheckConstraint("CK_importacoes_periodo", "periodo_inicio <= periodo_fim");
            tabela.HasCheckConstraint("CK_importacoes_lidas", "lidas >= 0");
        });

        entity.HasKey(i => i.Id);

        entity.Property(i => i.Id)
            .HasColumnName("id");

        entity.Property(i => i.IdUsuario)
            .HasColumnName("id_usuario");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(i => i.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        // Complex type, e não owned, como a origem da transação. O EF não aceita complex type em FK nem em índice, então
        // as FKs para contas e cartões e o índice com o fim do período vão por SQL na migration.
        entity.ComplexProperty(i => i.Origem, origem =>
        {
            origem.Property(o => o.IdConta)
                .HasColumnName("id_conta");

            origem.Property(o => o.IdCartao)
                .HasColumnName("id_cartao");
        });

        entity.Property(i => i.Formato)
            .HasColumnName("formato");

        entity.ComplexProperty(i => i.Periodo, periodo =>
        {
            periodo.Property(p => p.Inicio)
                .HasColumnName("periodo_inicio");

            periodo.Property(p => p.Fim)
                .HasColumnName("periodo_fim");
        });

        entity.Property(i => i.Lidas)
            .HasColumnName("lidas");

        entity.Property(i => i.ImportadaEm)
            .HasColumnName("importada_em");
    }
}
