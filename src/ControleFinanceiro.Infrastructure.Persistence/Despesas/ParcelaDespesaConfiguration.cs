using Ardalis.SmartEnum.EFCore;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class ParcelaDespesaConfiguration : IEntityTypeConfiguration<ParcelaDespesa>
{
    public void Configure(EntityTypeBuilder<ParcelaDespesa> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("parcelas_despesa", tabela =>
        {
            tabela.HasCheckConstraint("CK_parcelas_despesa_numero", "numero_parcela >= 1");
            tabela.HasCheckConstraint("CK_parcelas_despesa_valor_projetado", "valor_pagamento_projetado >= 0");
            tabela.HasCheckConstraint("CK_parcelas_despesa_valor_efetivo", "valor_pagamento_efetivo >= 0");
            tabela.HasCheckConstraint(
                "CK_parcelas_despesa_pagamento",
                $"(status_pagamento = {StatusPagamento.Pago.Value}) = (data_pagamento_efetiva IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_parcelas_despesa_pagamento_valor_efetivo",
                $"(status_pagamento = {StatusPagamento.Pago.Value}) = (valor_pagamento_efetivo IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_parcelas_despesa_pagamento_transacao",
                $"id_transacao_pagamento IS NULL OR status_pagamento = {StatusPagamento.Pago.Value}");
            tabela.HasCheckConstraint(
                "CK_parcelas_despesa_pagamento_mes",
                "mes_referencia = date_trunc('month', data_pagamento_projetada::timestamp)::date");
        });
        entity.HasKey(p => p.Id);

        entity.Property(p => p.Id)
            .HasColumnName("id");

        entity.Property(p => p.NumeroParcela)
            .HasColumnName("numero_parcela");

        entity.Property<IdDespesa>("id_despesa")
            .IsRequired();

        // Complex type, como o da DespesaInteira: o pagamento é um value object sem identidade.
        entity.ComplexProperty(p => p.Pagamento, pagamento =>
        {
            pagamento.Property(p => p.MesReferencia)
                .HasColumnName("mes_referencia");

            pagamento.Property(p => p.ValorProjetado)
                .HasColumnName("valor_pagamento_projetado");

            pagamento.Property(p => p.ValorEfetivo)
                .HasColumnName("valor_pagamento_efetivo");

            // A FK para transacoes e o índice único vão por SQL na migration, porque o EF não aceita complex type neles.
            pagamento.Property(p => p.IdTransacao)
                .HasColumnName("id_transacao_pagamento");

            // O ConfigureSmartEnum do DbContext não alcança as propriedades de complex type.
            pagamento.Property(p => p.Status)
                .HasColumnName("status_pagamento")
                .HasConversion<SmartEnumConverter<StatusPagamento, int>>();

            pagamento.Property(p => p.DataProjetada)
                .HasColumnName("data_pagamento_projetada");

            pagamento.Property(p => p.DataEfetiva)
                .HasColumnName("data_pagamento_efetiva");
        });

        entity.HasIndex("id_despesa", nameof(ParcelaDespesa.NumeroParcela))
            .IsUnique();
    }
}
