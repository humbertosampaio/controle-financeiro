using Ardalis.SmartEnum.EFCore;
using ControleFinanceiro.Domain.Despesas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaInteiraConfiguration : IEntityTypeConfiguration<DespesaInteira>
{
    public void Configure(EntityTypeBuilder<DespesaInteira> entity)
    {
        // Complex type, como a origem da transação: o pagamento é um value object sem identidade.
        entity.ComplexProperty(d => d.Pagamento, pagamento =>
        {
            pagamento.Property(p => p.MesReferencia)
                .HasColumnName("mes_referencia");

            pagamento.Property(p => p.ValorProjetado)
                .HasColumnName("valor_pagamento_projetado");

            pagamento.Property(p => p.ValorEfetivo)
                .HasColumnName("valor_pagamento_efetivo");

            // A FK para transacoes e os índices vão por SQL na migration, porque o EF não aceita complex type neles.
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

        // A geração cria só as despesas que faltam no mês; o índice único (id_despesa_recorrente, mes_referencia) é a
        // rede para duas gerações ao mesmo tempo. Ele continua no banco, criado pela migration Despesas, mas saiu do
        // modelo, porque o EF não aceita complex type em índice, como o de id_transacao_pagamento.
    }
}
