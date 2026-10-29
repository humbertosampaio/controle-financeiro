using Ardalis.SmartEnum.EFCore;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaInteiraConfiguration : IEntityTypeConfiguration<DespesaInteira>
{
    public void Configure(EntityTypeBuilder<DespesaInteira> entity)
    {
        entity.Property(d => d.MesReferencia)
            .HasColumnName("mes_referencia");

        // Complex type, como a origem da transação: o pagamento é um value object sem identidade.
        entity.ComplexProperty(d => d.Pagamento, pagamento =>
        {
            pagamento.Property(p => p.Valor)
                .HasColumnName("valor");

            // O ConfigureSmartEnum do DbContext não alcança as propriedades de complex type.
            pagamento.Property(p => p.Status)
                .HasColumnName("status_pagamento")
                .HasConversion<SmartEnumConverter<StatusPagamento, int>>();

            pagamento.Property(p => p.DataProjetada)
                .HasColumnName("data_pagamento_projetada");

            pagamento.Property(p => p.DataEfetiva)
                .HasColumnName("data_pagamento_efetiva");
        });

        // A geração cria só as despesas que faltam no mês; o índice é a rede para duas gerações ao mesmo tempo.
        entity.HasIndex(d => new { d.IdDespesaRecorrente, d.MesReferencia })
            .IsUnique();
    }
}
