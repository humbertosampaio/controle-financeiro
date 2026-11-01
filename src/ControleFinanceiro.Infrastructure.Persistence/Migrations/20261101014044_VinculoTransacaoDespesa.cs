using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VinculoTransacaoDespesa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_valor",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_valor",
                table: "despesas");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "parcelas_despesa",
                newName: "valor_pagamento_projetado");

            migrationBuilder.RenameColumn(
                name: "valor",
                table: "despesas",
                newName: "valor_pagamento_projetado");

            migrationBuilder.AddColumn<Guid>(
                name: "id_transacao_pagamento",
                table: "parcelas_despesa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_pagamento_efetivo",
                table: "parcelas_despesa",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id_transacao_pagamento",
                table: "despesas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_pagamento_efetivo",
                table: "despesas",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            // O que já está pago saiu pelo valor projetado, a única informação que se tem dele. Vem antes dos CHECK, que exigem o efetivo.
            migrationBuilder.Sql("""
                UPDATE despesas SET valor_pagamento_efetivo = valor_pagamento_projetado WHERE status_pagamento = 1;
                UPDATE parcelas_despesa SET valor_pagamento_efetivo = valor_pagamento_projetado WHERE status_pagamento = 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_transacao",
                table: "parcelas_despesa",
                sql: "id_transacao_pagamento IS NULL OR status_pagamento = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_valor_efetivo",
                table: "parcelas_despesa",
                sql: "(status_pagamento = 1) = (valor_pagamento_efetivo IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_valor_efetivo",
                table: "parcelas_despesa",
                sql: "valor_pagamento_efetivo >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_valor_projetado",
                table: "parcelas_despesa",
                sql: "valor_pagamento_projetado >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_pagamento_transacao",
                table: "despesas",
                sql: "id_transacao_pagamento IS NULL OR status_pagamento = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_pagamento_valor_efetivo",
                table: "despesas",
                sql: "(status_pagamento = 1) = (valor_pagamento_efetivo IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_valor_efetivo",
                table: "despesas",
                sql: "valor_pagamento_efetivo >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_valor_projetado",
                table: "despesas",
                sql: "valor_pagamento_projetado >= 0");

            // Por SQL porque o id da transação está em complex type. Adiada: a cascata do usuário apaga a transação antes da
            // parcela, que fica um nível mais longe dele, e a FK conferida no fim de cada comando interno quebraria. O Postgres
            // trata nulos como distintos, então o índice único não precisa ser parcial.
            migrationBuilder.Sql("""
                ALTER TABLE despesas
                    ADD CONSTRAINT "FK_despesas_transacoes_id_transacao_pagamento"
                    FOREIGN KEY (id_transacao_pagamento) REFERENCES transacoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE parcelas_despesa
                    ADD CONSTRAINT "FK_parcelas_despesa_transacoes_id_transacao_pagamento"
                    FOREIGN KEY (id_transacao_pagamento) REFERENCES transacoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE UNIQUE INDEX "IX_despesas_id_transacao_pagamento" ON despesas (id_transacao_pagamento);
                CREATE UNIQUE INDEX "IX_parcelas_despesa_id_transacao_pagamento" ON parcelas_despesa (id_transacao_pagamento);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX "IX_parcelas_despesa_id_transacao_pagamento";
                DROP INDEX "IX_despesas_id_transacao_pagamento";

                ALTER TABLE parcelas_despesa DROP CONSTRAINT "FK_parcelas_despesa_transacoes_id_transacao_pagamento";
                ALTER TABLE despesas DROP CONSTRAINT "FK_despesas_transacoes_id_transacao_pagamento";
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_transacao",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_valor_efetivo",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_valor_efetivo",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_valor_projetado",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_pagamento_transacao",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_pagamento_valor_efetivo",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_valor_efetivo",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_valor_projetado",
                table: "despesas");

            migrationBuilder.DropColumn(
                name: "id_transacao_pagamento",
                table: "parcelas_despesa");

            migrationBuilder.DropColumn(
                name: "valor_pagamento_efetivo",
                table: "parcelas_despesa");

            migrationBuilder.DropColumn(
                name: "id_transacao_pagamento",
                table: "despesas");

            migrationBuilder.DropColumn(
                name: "valor_pagamento_efetivo",
                table: "despesas");

            migrationBuilder.RenameColumn(
                name: "valor_pagamento_projetado",
                table: "parcelas_despesa",
                newName: "valor");

            migrationBuilder.RenameColumn(
                name: "valor_pagamento_projetado",
                table: "despesas",
                newName: "valor");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_valor",
                table: "parcelas_despesa",
                sql: "valor >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_valor",
                table: "despesas",
                sql: "valor >= 0");
        }
    }
}
