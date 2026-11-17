using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControleFinanceiro.Infrastructure.Persistence.Importacoes;

/// <summary>
/// Uma tabela para toda a hierarquia (TPH), com o tipo na coluna <c>tipo</c>.
/// </summary>
internal sealed class ImportacaoArquivoConfiguration : IEntityTypeConfiguration<ImportacaoArquivo>
{
    internal const string TipoExtrato = "extrato";
    internal const string TipoFatura = "fatura";

    public void Configure(EntityTypeBuilder<ImportacaoArquivo> entity)
    {
        // Os CHECK repetem no esquema o que o domínio já garante; a API é a única porta de entrada dos dados.
        entity.ToTable("importacoes", tabela =>
        {
            tabela.HasCheckConstraint(
                "CK_importacoes_origem",
                $"(tipo = '{TipoExtrato}' AND id_conta IS NOT NULL AND id_cartao IS NULL) OR (tipo = '{TipoFatura}' AND id_cartao IS NOT NULL AND id_conta IS NULL)");
            tabela.HasCheckConstraint("CK_importacoes_periodo", "periodo_inicio <= periodo_fim");
            tabela.HasCheckConstraint("CK_importacoes_lidas", "lidas >= 0");
            tabela.HasCheckConstraint(
                "CK_importacoes_vencimento_fatura",
                $"(tipo = '{TipoFatura}') = (data_vencimento_fatura IS NOT NULL)");
        });

        entity.HasDiscriminator<string>("tipo")
            .HasValue<ImportacaoExtrato>(TipoExtrato)
            .HasValue<ImportacaoFatura>(TipoFatura);

        entity.HasKey(i => i.Id);

        entity.Property(i => i.Id)
            .HasColumnName("id");

        entity.Property(i => i.IdUsuario)
            .HasColumnName("id_usuario");

        entity.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(i => i.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(i => i.Layout)
            .HasColumnName("layout");

        entity.ComplexProperty(i => i.Periodo, periodo =>
        {
            periodo.Property(p => p.Inicio)
                .HasColumnName("periodo_inicio");

            periodo.Property(p => p.Fim)
                .HasColumnName("periodo_fim");
        });

        entity.Property(i => i.Lidas)
            .HasColumnName("lidas");

        entity.Property(i => i.ImportadaEm)
            .HasColumnName("importada_em");
    }
}
