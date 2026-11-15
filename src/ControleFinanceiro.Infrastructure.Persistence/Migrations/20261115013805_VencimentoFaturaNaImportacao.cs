using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VencimentoFaturaNaImportacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cartoes_dia_vencimento",
                table: "cartoes");

            migrationBuilder.AddColumn<DateOnly>(
                name: "mes_vencimento_fatura",
                table: "transacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "data_vencimento_fatura",
                table: "importacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes",
                sql: "(id_cartao IS NULL) = (mes_vencimento_fatura IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes",
                sql: "(id_cartao IS NULL) = (data_vencimento_fatura IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cartoes_dia_vencimento",
                table: "cartoes",
                sql: "dia_vencimento BETWEEN 1 AND 24");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_mes_vencimento_fatura",
                table: "transacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_importacoes_vencimento_fatura",
                table: "importacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_cartoes_dia_vencimento",
                table: "cartoes");

            migrationBuilder.DropColumn(
                name: "mes_vencimento_fatura",
                table: "transacoes");

            migrationBuilder.DropColumn(
                name: "data_vencimento_fatura",
                table: "importacoes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_cartoes_dia_vencimento",
                table: "cartoes",
                sql: "dia_vencimento BETWEEN 1 AND 28");
        }
    }
}
