using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Despesas;

internal sealed class DespesaRecorrenteConfiguration : IEntityTypeConfiguration<DespesaRecorrente>
{
    public void Configure(EntityTypeBuilder<DespesaRecorrente> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados. A
        // recorrente repetida (mesmo beneficiário e descrição normalizada) só a factory recusa, sem índice único.
        entity.ToTable("despesas_recorrentes", tabela =>
        {
            tabela.HasCheckConstraint("CK_despesas_recorrentes_descricao", "btrim(descricao) <> ''");
            tabela.HasCheckConstraint("CK_despesas_recorrentes_valor", "valor >= 0");
            tabela.HasCheckConstraint("CK_despesas_recorrentes_dia_ocorrencia", "dia_ocorrencia BETWEEN 1 AND 28");
            tabela.HasCheckConstraint(
                "CK_despesas_recorrentes_cartao",
                $"(forma_pagamento = {FormaPagamento.Credito.Value}) = (id_cartao IS NOT NULL)");
        });

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
        // que apaga o beneficiário, a categoria, o cartão e as recorrentes no mesmo comando.
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

        entity.Property(d => d.FormaPagamento)
            .HasColumnName("forma_pagamento");

        entity.Property(d => d.IdCartao)
            .HasColumnName("id_cartao");

        entity.HasOne<Cartao>()
            .WithMany()
            .HasForeignKey(d => d.IdCartao)
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(d => d.Valor)
            .HasColumnName("valor");

        entity.Property(d => d.DiaOcorrencia)
            .HasColumnName("dia_ocorrencia")
            .HasConversion(dia => dia.Value, valor => (DiaOcorrenciaRecorrente)valor);
    }
}
