using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Outbox;

/// <summary>
/// Sem FK: os ids dos agregados ficam no JSON do conteúdo. Fora do filtro do usuário, porque a mensagem não implementa
/// <see cref="Domain.Usuarios.IPertenceAoUsuario"/>.
/// </summary>
internal sealed class MensagemOutboxConfiguration : IEntityTypeConfiguration<MensagemOutbox>
{
    public void Configure(EntityTypeBuilder<MensagemOutbox> entity)
    {
        entity.ToTable("mensagens_outbox", tabela =>
            tabela.HasCheckConstraint("CK_mensagens_outbox_tentativas", "tentativas >= 0"));

        entity.HasKey(m => m.Id);

        entity.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        entity.Property(m => m.Tipo)
            .HasColumnName("tipo")
            .HasMaxLength(100);

        entity.Property(m => m.Conteudo)
            .HasColumnName("conteudo")
            .HasColumnType("jsonb");

        entity.Property(m => m.RegistradaEm)
            .HasColumnName("registrada_em");

        entity.Property(m => m.ProcessadaEm)
            .HasColumnName("processada_em");

        entity.Property(m => m.Tentativas)
            .HasColumnName("tentativas")
            .HasDefaultValue(0);

        entity.Property(m => m.UltimoErro)
            .HasColumnName("ultimo_erro")
            .HasMaxLength(2000);

        // A busca da próxima pendente lê só as não processadas, das menos tentadas para as mais, e as mais antigas antes.
        entity.HasIndex(m => new { m.Tentativas, m.RegistradaEm })
            .HasDatabaseName("IX_mensagens_outbox_pendentes")
            .HasFilter("processada_em IS NULL");
    }
}
