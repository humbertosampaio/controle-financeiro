using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Transacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

internal sealed class TransacaoCartaoConfiguration : IEntityTypeConfiguration<TransacaoCartao>
{
    public void Configure(EntityTypeBuilder<TransacaoCartao> entity)
    {
        entity.Property(t => t.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(t => t.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);

        entity.HasIndex(t => new { t.IdCartao, t.Data });

        // O conversor global grava o mês como o primeiro dia dele.
        entity.Property(t => t.MesVencimentoFatura)
            .HasColumnName("mes_vencimento_fatura");

        // Complex type opcional: a compra à vista não tem parcela, e as duas colunas ficam nulas.
        entity.ComplexProperty(t => t.Parcela, parcela =>
        {
            parcela.Property(p => p.Numero)
                .HasColumnName("parcela_numero");

            parcela.Property(p => p.Total)
                .HasColumnName("parcela_total");
        });

        entity.Property(t => t.EhPagamentoFaturaAnterior)
            .HasColumnName("eh_pagamento_fatura_anterior");
    }
}
