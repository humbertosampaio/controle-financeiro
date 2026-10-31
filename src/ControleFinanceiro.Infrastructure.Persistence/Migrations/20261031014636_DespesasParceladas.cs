using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DespesasParceladas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_cartao",
                table: "despesas");

            migrationBuilder.CreateTable(
                name: "parcelas_despesa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_parcela = table.Column<int>(type: "integer", nullable: false),
                    mes_referencia = table.Column<DateOnly>(type: "date", nullable: false),
                    id_despesa = table.Column<Guid>(type: "uuid", nullable: false),
                    data_pagamento_efetiva = table.Column<DateOnly>(type: "date", nullable: true),
                    data_pagamento_projetada = table.Column<DateOnly>(type: "date", nullable: true),
                    status_pagamento = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcelas_despesa", x => x.id);
                    table.CheckConstraint("CK_parcelas_despesa_numero", "numero_parcela >= 1");
                    table.CheckConstraint("CK_parcelas_despesa_pagamento", "(status_pagamento = 1) = (data_pagamento_efetiva IS NOT NULL)");
                    table.CheckConstraint("CK_parcelas_despesa_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "FK_parcelas_despesa_despesas_id_despesa",
                        column: x => x.id_despesa,
                        principalTable: "despesas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_cartao",
                table: "despesas",
                sql: "(tipo IN ('adiada', 'parcelada')) = (id_cartao IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_parcelas_despesa_id_despesa_numero_parcela",
                table: "parcelas_despesa",
                columns: new[] { "id_despesa", "numero_parcela" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_cartao",
                table: "despesas");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_cartao",
                table: "despesas",
                sql: "(tipo = 'adiada') = (id_cartao IS NOT NULL)");
        }
    }
}
