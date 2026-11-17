using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransacoesPorTipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_origem",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_pagamento_fatura",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes");

            // As FKs e os índices que a ContasTransacoesEImportacoes criou por SQL saem: voltam pelo modelo, com os mesmos nomes.
            migrationBuilder.Sql("""
                ALTER TABLE transacoes DROP CONSTRAINT "FK_transacoes_cartoes_id_cartao";
                ALTER TABLE transacoes DROP CONSTRAINT "FK_transacoes_contas_id_conta";

                DROP INDEX "IX_transacoes_id_cartao_data";
                DROP INDEX "IX_transacoes_id_conta_data";
                """);

            // A coluna nasce aceitando nulo, é preenchida pela origem de cada linha e só então passa a NOT NULL, sem default.
            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "transacoes",
                type: "character varying(13)",
                maxLength: 13,
                nullable: true);

            migrationBuilder.Sql("UPDATE transacoes SET tipo = CASE WHEN id_cartao IS NULL THEN 'conta' ELSE 'cartao' END;");

            migrationBuilder.AlterColumn<string>(
                name: "tipo",
                table: "transacoes",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13,
                oldNullable: true);

            // A marca é só do cartão: a coluna passa a aceitar nulo e perde o DEFAULT false, que quebraria o CHECK novo no
            // insert da transação da conta, em que o EF não manda a coluna. Nas linhas da conta ela era sempre false.
            migrationBuilder.RenameColumn(
                name: "eh_pagamento_fatura",
                table: "transacoes",
                newName: "eh_pagamento_fatura_anterior");

            migrationBuilder.AlterColumn<bool>(
                name: "eh_pagamento_fatura_anterior",
                table: "transacoes",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.Sql("UPDATE transacoes SET eh_pagamento_fatura_anterior = NULL WHERE tipo = 'conta';");

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_id_cartao_data",
                table: "transacoes",
                columns: new[] { "id_cartao", "data" });

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_id_conta_data",
                table: "transacoes",
                columns: new[] { "id_conta", "data" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes",
                sql: "(tipo = 'cartao') = (mes_vencimento_fatura IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_origem",
                table: "transacoes",
                sql: "(tipo = 'conta' AND id_conta IS NOT NULL AND id_cartao IS NULL) OR (tipo = 'cartao' AND id_cartao IS NOT NULL AND id_conta IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_pagamento_fatura_anterior",
                table: "transacoes",
                sql: "(tipo = 'cartao') = (eh_pagamento_fatura_anterior IS NOT NULL) AND (eh_pagamento_fatura_anterior IS NOT TRUE OR (valor >= 0 AND parcela_numero IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes",
                sql: "num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total AND tipo = 'cartao')");

            migrationBuilder.AddForeignKey(
                name: "FK_transacoes_cartoes_id_cartao",
                table: "transacoes",
                column: "id_cartao",
                principalTable: "cartoes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_transacoes_contas_id_conta",
                table: "transacoes",
                column: "id_conta",
                principalTable: "contas",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transacoes_cartoes_id_cartao",
                table: "transacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_transacoes_contas_id_conta",
                table: "transacoes");

            migrationBuilder.DropIndex(
                name: "IX_transacoes_id_cartao_data",
                table: "transacoes");

            migrationBuilder.DropIndex(
                name: "IX_transacoes_id_conta_data",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_origem",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_pagamento_fatura_anterior",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes");

            migrationBuilder.Sql("UPDATE transacoes SET eh_pagamento_fatura_anterior = false WHERE eh_pagamento_fatura_anterior IS NULL;");

            migrationBuilder.AlterColumn<bool>(
                name: "eh_pagamento_fatura_anterior",
                table: "transacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "eh_pagamento_fatura_anterior",
                table: "transacoes",
                newName: "eh_pagamento_fatura");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "transacoes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes",
                sql: "(id_cartao IS NULL) = (mes_vencimento_fatura IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_origem",
                table: "transacoes",
                sql: "num_nonnulls(id_conta, id_cartao) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_pagamento_fatura",
                table: "transacoes",
                sql: "NOT eh_pagamento_fatura OR (id_cartao IS NOT NULL AND valor >= 0 AND parcela_numero IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes",
                sql: "num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total AND id_cartao IS NOT NULL)");

            migrationBuilder.Sql("""
                ALTER TABLE transacoes
                    ADD CONSTRAINT "FK_transacoes_contas_id_conta" FOREIGN KEY (id_conta) REFERENCES contas (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE transacoes
                    ADD CONSTRAINT "FK_transacoes_cartoes_id_cartao" FOREIGN KEY (id_cartao) REFERENCES cartoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE INDEX "IX_transacoes_id_conta_data" ON transacoes (id_conta, data);

                CREATE INDEX "IX_transacoes_id_cartao_data" ON transacoes (id_cartao, data);
                """);
        }
    }
}
