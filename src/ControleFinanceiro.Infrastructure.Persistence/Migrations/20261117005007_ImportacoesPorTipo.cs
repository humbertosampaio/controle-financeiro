using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImportacoesPorTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_importacoes_origem",
                table: "importacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes");

            // As FKs e os índices que a ContasTransacoesEImportacoes criou por SQL saem: as FKs voltam pelo modelo, com os
            // mesmos nomes, e os índices com o fim do período dão lugar aos da FK.
            migrationBuilder.Sql("""
                ALTER TABLE importacoes DROP CONSTRAINT "FK_importacoes_cartoes_id_cartao";
                ALTER TABLE importacoes DROP CONSTRAINT "FK_importacoes_contas_id_conta";

                DROP INDEX "IX_importacoes_id_cartao_periodo_fim";
                DROP INDEX "IX_importacoes_id_conta_periodo_fim";
                """);

            // A coluna nasce aceitando nulo, é preenchida pela origem de cada linha e só então passa a NOT NULL, sem default.
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "importacoes",
                type: "character varying(21)",
                maxLength: 21,
                nullable: true);

            migrationBuilder.Sql("UPDATE importacoes SET tipo = CASE WHEN id_cartao IS NULL THEN 'extrato' ELSE 'fatura' END;");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "importacoes",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_id_cartao",
                table: "importacoes",
                column: "id_cartao");

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_id_conta",
                table: "importacoes",
                column: "id_conta");

            migrationBuilder.AddCheckConstraint(
                name: "CK_importacoes_origem",
                table: "importacoes",
                sql: "(tipo = 'extrato' AND id_conta IS NOT NULL AND id_cartao IS NULL) OR (tipo = 'fatura' AND id_cartao IS NOT NULL AND id_conta IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes",
                sql: "(tipo = 'fatura') = (data_vencimento_fatura IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_importacoes_cartoes_id_cartao",
                table: "importacoes",
                column: "id_cartao",
                principalTable: "cartoes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_importacoes_contas_id_conta",
                table: "importacoes",
                column: "id_conta",
                principalTable: "contas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_importacoes_cartoes_id_cartao",
                table: "importacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_importacoes_contas_id_conta",
                table: "importacoes");

            migrationBuilder.DropIndex(
                name: "IX_importacoes_id_cartao",
                table: "importacoes");

            migrationBuilder.DropIndex(
                name: "IX_importacoes_id_conta",
                table: "importacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_importacoes_origem",
                table: "importacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "importacoes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_importacoes_origem",
                table: "importacoes",
                sql: "num_nonnulls(id_conta, id_cartao) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes",
                sql: "(id_cartao IS NULL) = (data_vencimento_fatura IS NULL)");

            migrationBuilder.Sql("""
                ALTER TABLE importacoes
                    ADD CONSTRAINT "FK_importacoes_contas_id_conta" FOREIGN KEY (id_conta) REFERENCES contas (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE importacoes
                    ADD CONSTRAINT "FK_importacoes_cartoes_id_cartao" FOREIGN KEY (id_cartao) REFERENCES cartoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE INDEX "IX_importacoes_id_conta_periodo_fim" ON importacoes (id_conta, periodo_fim);

                CREATE INDEX "IX_importacoes_id_cartao_periodo_fim" ON importacoes (id_cartao, periodo_fim);
                """);
        }
    }
}
