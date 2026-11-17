using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Transacoes;

/// <summary>
/// Uma tabela para toda a hierarquia (TPH), com o tipo na coluna <c>tipo</c>.
/// </summary>
internal sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    internal const string TipoConta = "conta";
    internal const string TipoCartao = "cartao";

    public void Configure(EntityTypeBuilder<Transacao> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("transacoes", tabela =>
        {
            tabela.HasCheckConstraint(
                "CK_transacoes_origem",
                $"(tipo = '{TipoConta}' AND id_conta IS NOT NULL AND id_cartao IS NULL) OR (tipo = '{TipoCartao}' AND id_cartao IS NOT NULL AND id_conta IS NULL)");
            tabela.HasCheckConstraint("CK_transacoes_chave_deduplicacao", "chave_deduplicacao ~ '^[0-9a-f]{64}$'");
            // O num_nulls = 0 na segunda parte não é redundante: a CHECK passa quando a expressão dá NULL, e uma parcela
            // com só uma das colunas preenchida daria NULL sem ele.
            tabela.HasCheckConstraint(
                "CK_transacoes_parcela",
                $"num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total AND tipo = '{TipoCartao}')");
            tabela.HasCheckConstraint("CK_transacoes_mes_vencimento_fatura", $"(tipo = '{TipoCartao}') = (mes_vencimento_fatura IS NOT NULL)");
            // A marca é de uma filha só, e o TPH deixa a coluna aceitar nulo: ela vem preenchida em toda linha do cartão.
            tabela.HasCheckConstraint(
                "CK_transacoes_pagamento_fatura_anterior",
                $"(tipo = '{TipoCartao}') = (eh_pagamento_fatura_anterior IS NOT NULL) AND (eh_pagamento_fatura_anterior IS NOT TRUE OR (valor >= 0 AND parcela_numero IS NULL))");
        });

        entity.HasDiscriminator<string>("tipo")
            .HasValue<TransacaoConta>(TipoConta)
            .HasValue<TransacaoCartao>(TipoCartao);

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
        entity.HasOne<ImportacaoArquivo>()
            .WithMany()
            .HasForeignKey(t => t.IdImportacao)
            .OnDelete(DeleteBehavior.NoAction);

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

        entity.Property(t => t.CategoriaBanco)
            .HasColumnName("categoria_banco");

        entity.Property(t => t.Chave)
            .HasColumnName("chave_deduplicacao")
            .HasConversion(chave => chave.Value, valor => (ChaveDeduplicacao)valor)
            .HasMaxLength(ChaveDeduplicacao.QuantidadeCaracteres)
            .IsFixedLength();

        entity.HasIndex(t => t.Chave)
            .IsUnique();
    }
}
