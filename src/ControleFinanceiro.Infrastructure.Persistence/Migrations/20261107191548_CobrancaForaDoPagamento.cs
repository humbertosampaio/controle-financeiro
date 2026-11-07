using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CobrancaForaDoPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "id_transacao_cobranca",
                table: "parcelas_despesa",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_cobrado",
                table: "parcelas_despesa",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "id_transacao_cobranca",
                table: "despesas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_cobrado",
                table: "despesas",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            // O que hoje está no pagamento das despesas do cartão é a cobrança: a compra da fatura, com a data efetiva
            // suposta no vencimento. O pagamento volta a pendente, inclusive o da parcela que nasceu paga e foi cobrada
            // depois, que o banco não distingue. Vem antes dos CHECK, que exigem a cobrança da adiada paga.
            migrationBuilder.Sql("""
                UPDATE despesas
                   SET id_transacao_cobranca = id_transacao_pagamento,
                       valor_cobrado = valor_pagamento_efetivo,
                       id_transacao_pagamento = NULL,
                       valor_pagamento_efetivo = NULL,
                       data_pagamento_efetiva = NULL,
                       status_pagamento = 0
                 WHERE tipo = 'adiada' AND id_transacao_pagamento IS NOT NULL;

                UPDATE parcelas_despesa
                   SET id_transacao_cobranca = id_transacao_pagamento,
                       valor_cobrado = valor_pagamento_efetivo,
                       id_transacao_pagamento = NULL,
                       valor_pagamento_efetivo = NULL,
                       data_pagamento_efetiva = NULL,
                       status_pagamento = 0
                 WHERE id_transacao_pagamento IS NOT NULL;
                """);

            // Por SQL porque os ids estão em complex type. Um pagamento de fatura paga várias despesas, então o índice de
            // id_transacao_pagamento deixa de ser único; a saída paga uma simples só, com o índice parcial como rede. A FK
            // da cobrança é adiada pelo mesmo motivo da do pagamento (migration VinculoTransacaoDespesa).
            migrationBuilder.Sql("""
                DROP INDEX "IX_despesas_id_transacao_pagamento";
                DROP INDEX "IX_parcelas_despesa_id_transacao_pagamento";
                CREATE INDEX "IX_despesas_id_transacao_pagamento" ON despesas (id_transacao_pagamento);
                CREATE INDEX "IX_parcelas_despesa_id_transacao_pagamento" ON parcelas_despesa (id_transacao_pagamento);
                CREATE UNIQUE INDEX "IX_despesas_id_transacao_pagamento_simples" ON despesas (id_transacao_pagamento)
                    WHERE tipo = 'simples';

                ALTER TABLE despesas
                    ADD CONSTRAINT "FK_despesas_transacoes_id_transacao_cobranca"
                    FOREIGN KEY (id_transacao_cobranca) REFERENCES transacoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE parcelas_despesa
                    ADD CONSTRAINT "FK_parcelas_despesa_transacoes_id_transacao_cobranca"
                    FOREIGN KEY (id_transacao_cobranca) REFERENCES transacoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE UNIQUE INDEX "IX_despesas_id_transacao_cobranca" ON despesas (id_transacao_cobranca);
                CREATE UNIQUE INDEX "IX_parcelas_despesa_id_transacao_cobranca" ON parcelas_despesa (id_transacao_cobranca);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_cobranca",
                table: "parcelas_despesa",
                sql: "(id_transacao_cobranca IS NULL) = (valor_cobrado IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_paga_cobrada",
                table: "parcelas_despesa",
                sql: "id_transacao_pagamento IS NULL OR id_transacao_cobranca IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_valor_cobrado",
                table: "parcelas_despesa",
                sql: "valor_cobrado >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_adiada_paga_cobrada",
                table: "despesas",
                sql: "tipo <> 'adiada' OR status_pagamento <> 1 OR id_transacao_cobranca IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_cobranca",
                table: "despesas",
                sql: "(id_transacao_cobranca IS NULL) = (valor_cobrado IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_cobranca_so_adiada",
                table: "despesas",
                sql: "id_transacao_cobranca IS NULL OR tipo = 'adiada'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_valor_cobrado",
                table: "despesas",
                sql: "valor_cobrado >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A cobrança volta para o pagamento, como pago e com a data projetada como efetiva, a convenção de antes. Falha
            // se uma saída pagou mais de uma despesa, pelo índice único recriado.
            migrationBuilder.Sql("""
                DROP INDEX "IX_parcelas_despesa_id_transacao_cobranca";
                DROP INDEX "IX_despesas_id_transacao_cobranca";
                ALTER TABLE parcelas_despesa DROP CONSTRAINT "FK_parcelas_despesa_transacoes_id_transacao_cobranca";
                ALTER TABLE despesas DROP CONSTRAINT "FK_despesas_transacoes_id_transacao_cobranca";

                DROP INDEX "IX_despesas_id_transacao_pagamento_simples";
                DROP INDEX "IX_parcelas_despesa_id_transacao_pagamento";
                DROP INDEX "IX_despesas_id_transacao_pagamento";

                UPDATE despesas
                   SET id_transacao_pagamento = id_transacao_cobranca,
                       valor_pagamento_efetivo = valor_cobrado,
                       data_pagamento_efetiva = data_pagamento_projetada,
                       status_pagamento = 1
                 WHERE id_transacao_cobranca IS NOT NULL;

                UPDATE parcelas_despesa
                   SET id_transacao_pagamento = id_transacao_cobranca,
                       valor_pagamento_efetivo = valor_cobrado,
                       data_pagamento_efetiva = data_pagamento_projetada,
                       status_pagamento = 1
                 WHERE id_transacao_cobranca IS NOT NULL;

                CREATE UNIQUE INDEX "IX_despesas_id_transacao_pagamento" ON despesas (id_transacao_pagamento);
                CREATE UNIQUE INDEX "IX_parcelas_despesa_id_transacao_pagamento" ON parcelas_despesa (id_transacao_pagamento);
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_cobranca",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_paga_cobrada",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_valor_cobrado",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_adiada_paga_cobrada",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_cobranca",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_cobranca_so_adiada",
                table: "despesas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_valor_cobrado",
                table: "despesas");

            migrationBuilder.DropColumn(
                name: "id_transacao_cobranca",
                table: "parcelas_despesa");

            migrationBuilder.DropColumn(
                name: "valor_cobrado",
                table: "parcelas_despesa");

            migrationBuilder.DropColumn(
                name: "id_transacao_cobranca",
                table: "despesas");

            migrationBuilder.DropColumn(
                name: "valor_cobrado",
                table: "despesas");
        }
    }
}
