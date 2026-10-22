using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("transacoes", tabela =>
        {
            tabela.HasCheckConstraint("CK_transacoes_origem", "num_nonnulls(id_conta, id_cartao) = 1");
            tabela.HasCheckConstraint("CK_transacoes_chave_deduplicacao", "chave_deduplicacao ~ '^[0-9a-f]{64}$'");
        });

        entity.HasKey(t => t.Id);

        entity.Property(t => t.Id)
            .HasColumnName("id");

        entity.Property(t => t.IdUsuario)
            .HasColumnName("id_usuario");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(t => t.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(t => t.IdImportacao)
            .HasColumnName("id_importacao");

        // NoAction, e não Restrict: no Postgres, o Restrict confere na hora e quebraria a exclusão em cascata do usuário,
        // que apaga as importações e as transações no mesmo comando.
        entity.HasOne<ImportacaoExtrato>()
            .WithMany()
            .HasForeignKey(t => t.IdImportacao)
            .OnDelete(DeleteBehavior.NoAction);

        // Complex type, e não owned: o lote passa a mesma instância de origem a todas as transações, e o owned é
        // rastreado pela referência. As FKs para contas e cartões e os índices com a data vão por SQL na migration,
        // porque o EF não aceita complex type em FK nem em índice.
        entity.ComplexProperty(t => t.Origem, origem =>
        {
            origem.Property(o => o.IdConta)
                .HasColumnName("id_conta");

            origem.Property(o => o.IdCartao)
                .HasColumnName("id_cartao");
        });

        entity.Property(t => t.Data)
            .HasColumnName("data");

        entity.Property(t => t.Hora)
            .HasColumnName("hora");

        entity.Property(t => t.Valor)
            .HasColumnName("valor");

        entity.Property(t => t.Descricao)
            .HasColumnName("descricao");

        entity.Property(t => t.Contraparte)
            .HasColumnName("contraparte");

        entity.Property(t => t.Chave)
            .HasColumnName("chave_deduplicacao")
            .HasConversion(chave => chave.Value, valor => ChaveDeduplicacao.Criar(valor).Value)
            .HasMaxLength(ChaveDeduplicacao.QuantidadeCaracteres)
            .IsFixedLength();

        entity.HasIndex(t => t.Chave)
            .IsUnique();
    }
}
