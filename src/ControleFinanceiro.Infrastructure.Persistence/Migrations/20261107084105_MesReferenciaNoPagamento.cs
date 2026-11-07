using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MesReferenciaNoPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O índice único (id_despesa_recorrente, mes_referencia) saiu só do modelo, porque o EF não aceita complex type
            // em índice; o DropIndex gerado foi removido para ele continuar no banco.

            // Sem valor padrão: uma parcela sem data projetada faz a migration falhar, em vez de ganhar uma data inventada.
            migrationBuilder.AlterColumn<DateOnly>(
                name: "data_pagamento_projetada",
                table: "parcelas_despesa",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "data_pagamento_projetada",
                table: "parcelas_despesa",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            // O índice único continua no banco, porque o Up não o apaga.
        }
    }
}
