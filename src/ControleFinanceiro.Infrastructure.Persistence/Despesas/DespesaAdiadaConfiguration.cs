using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaAdiadaConfiguration : IEntityTypeConfiguration<DespesaAdiada>
{
    public void Configure(EntityTypeBuilder<DespesaAdiada> entity)
    {
        entity.Property(d => d.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(d => d.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);

        // Complex type opcional: sem cobrança, as duas colunas ficam nulas. A FK para transacoes e o índice único vão
        // por SQL na migration, porque o EF não aceita complex type neles.
        entity.ComplexProperty(d => d.Cobranca, cobranca =>
        {
            cobranca.IsRequired(false);

            cobranca.Property(c => c.IdTransacao)
                .HasColumnName("id_transacao_cobranca");

            cobranca.Property(c => c.Valor)
                .HasColumnName("valor_cobrado");
        });
    }
}
