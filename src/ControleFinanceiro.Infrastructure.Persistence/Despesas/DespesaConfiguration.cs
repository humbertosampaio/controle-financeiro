using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

/// <summary>
/// Uma tabela para toda a hierarquia (TPH), com o tipo na coluna <c>tipo</c>.
/// </summary>
internal sealed class DespesaConfiguration : IEntityTypeConfiguration<Despesa>
{
    internal const string TipoSimples = "simples";
    internal const string TipoAdiada = "adiada";
    internal const string TipoParcelada = "parcelada";

    public void Configure(EntityTypeBuilder<Despesa> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("despesas", tabela =>
        {
            tabela.HasCheckConstraint("CK_despesas_descricao", "btrim(descricao) <> ''");
            tabela.HasCheckConstraint("CK_despesas_valor_projetado", "valor_pagamento_projetado >= 0");
            tabela.HasCheckConstraint("CK_despesas_valor_efetivo", "valor_pagamento_efetivo >= 0");
            tabela.HasCheckConstraint(
                "CK_despesas_cartao",
                $"(tipo IN ('{TipoAdiada}', '{TipoParcelada}')) = (id_cartao IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_despesas_pagamento",
                $"(status_pagamento = {StatusPagamento.Pago.Value}) = (data_pagamento_efetiva IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_despesas_pagamento_valor_efetivo",
                $"(status_pagamento = {StatusPagamento.Pago.Value}) = (valor_pagamento_efetivo IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_despesas_pagamento_transacao",
                $"id_transacao_pagamento IS NULL OR status_pagamento = {StatusPagamento.Pago.Value}");

            tabela.HasCheckConstraint(
                "CK_despesas_cobranca",
                "(id_transacao_cobranca IS NULL) = (valor_cobrado IS NULL)");
            tabela.HasCheckConstraint("CK_despesas_valor_cobrado", "valor_cobrado >= 0");
            tabela.HasCheckConstraint(
                "CK_despesas_cobranca_so_adiada",
                $"id_transacao_cobranca IS NULL OR tipo = '{TipoAdiada}'");
            tabela.HasCheckConstraint(
                "CK_despesas_adiada_paga_cobrada",
                $"tipo <> '{TipoAdiada}' OR status_pagamento <> {StatusPagamento.Pago.Value} OR id_transacao_cobranca IS NOT NULL");

            // Nas linhas da parcelada, as duas colunas são nulas, e o CHECK com nulo passa.
            tabela.HasCheckConstraint(
                "CK_despesas_pagamento_mes",
                "mes_referencia = date_trunc('month', data_pagamento_projetada::timestamp)::date");
        });

        entity.HasDiscriminator<string>("tipo")
            .HasValue<DespesaSimples>(TipoSimples)
            .HasValue<DespesaAdiada>(TipoAdiada)
            .HasValue<DespesaParcelada>(TipoParcelada);

        entity.HasKey(d => d.Id);

        entity.Property(d => d.Id)
            .HasColumnName("id");

        entity.Property(d => d.IdUsuario)
            .HasColumnName("id_usuario");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(d => d.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(d => d.IdBeneficiario)
            .HasColumnName("id_beneficiario");

        // NoAction, e não Restrict: no Postgres, o Restrict confere na hora e quebraria a exclusão em cascata do usuário,
        // que apaga o beneficiário, a categoria, o cartão, as recorrentes e as despesas no mesmo comando.
        entity.HasOne<Beneficiario>()
            .WithMany()
            .HasForeignKey(d => d.IdBeneficiario)
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(d => d.Descricao)
            .HasColumnName("descricao");

        entity.Property(d => d.IdCategoria)
            .HasColumnName("id_categoria");

        entity.HasOne<CategoriaDespesa>()
            .WithMany()
            .HasForeignKey(d => d.IdCategoria)
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(d => d.DataOcorrencia)
            .HasColumnName("data_ocorrencia");

        entity.Property(d => d.FormaPagamento)
            .HasColumnName("forma_pagamento");

        entity.Property(d => d.IdDespesaRecorrente)
            .HasColumnName("id_despesa_recorrente");

        entity.HasOne<DespesaRecorrente>()
            .WithMany()
            .HasForeignKey(d => d.IdDespesaRecorrente)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
