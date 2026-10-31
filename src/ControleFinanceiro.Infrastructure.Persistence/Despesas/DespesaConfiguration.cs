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
            tabela.HasCheckConstraint("CK_despesas_valor", "valor >= 0");
            tabela.HasCheckConstraint(
                "CK_despesas_cartao",
                $"(tipo IN ('{TipoAdiada}', '{TipoParcelada}')) = (id_cartao IS NOT NULL)");
            tabela.HasCheckConstraint(
                "CK_despesas_pagamento",
                $"(status_pagamento = {StatusPagamento.Pago.Value}) = (data_pagamento_efetiva IS NOT NULL)");
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
