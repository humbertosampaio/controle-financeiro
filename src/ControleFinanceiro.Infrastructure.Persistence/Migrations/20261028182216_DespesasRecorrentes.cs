using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DespesasRecorrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "despesas_recorrentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_beneficiario = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    id_categoria = table.Column<Guid>(type: "uuid", nullable: false),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    id_cartao = table.Column<Guid>(type: "uuid", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    dia_ocorrencia = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_despesas_recorrentes", x => x.id);
                    table.CheckConstraint("CK_despesas_recorrentes_cartao", "(forma_pagamento = 2) = (id_cartao IS NOT NULL)");
                    table.CheckConstraint("CK_despesas_recorrentes_descricao", "btrim(descricao) <> ''");
                    table.CheckConstraint("CK_despesas_recorrentes_dia_ocorrencia", "dia_ocorrencia BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_despesas_recorrentes_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "FK_despesas_recorrentes_beneficiarios_id_beneficiario",
                        column: x => x.id_beneficiario,
                        principalTable: "beneficiarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_recorrentes_cartoes_id_cartao",
                        column: x => x.id_cartao,
                        principalTable: "cartoes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_recorrentes_categorias_despesa_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias_despesa",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_recorrentes_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_despesas_recorrentes_id_beneficiario",
                table: "despesas_recorrentes",
                column: "id_beneficiario");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_recorrentes_id_cartao",
                table: "despesas_recorrentes",
                column: "id_cartao");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_recorrentes_id_categoria",
                table: "despesas_recorrentes",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_recorrentes_id_usuario",
                table: "despesas_recorrentes",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "despesas_recorrentes");
        }
    }
}
