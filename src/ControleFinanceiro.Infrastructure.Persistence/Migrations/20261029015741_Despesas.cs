using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Despesas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "despesas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_beneficiario = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    id_categoria = table.Column<Guid>(type: "uuid", nullable: true),
                    data_ocorrencia = table.Column<DateOnly>(type: "date", nullable: false),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    id_despesa_recorrente = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    mes_referencia = table.Column<DateOnly>(type: "date", nullable: true),
                    data_pagamento_efetiva = table.Column<DateOnly>(type: "date", nullable: true),
                    data_pagamento_projetada = table.Column<DateOnly>(type: "date", nullable: true),
                    status_pagamento = table.Column<int>(type: "integer", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    id_cartao = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_despesas", x => x.id);
                    table.CheckConstraint("CK_despesas_cartao", "(tipo = 'adiada') = (id_cartao IS NOT NULL)");
                    table.CheckConstraint("CK_despesas_descricao", "btrim(descricao) <> ''");
                    table.CheckConstraint("CK_despesas_pagamento", "(status_pagamento = 1) = (data_pagamento_efetiva IS NOT NULL)");
                    table.CheckConstraint("CK_despesas_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "FK_despesas_beneficiarios_id_beneficiario",
                        column: x => x.id_beneficiario,
                        principalTable: "beneficiarios",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_cartoes_id_cartao",
                        column: x => x.id_cartao,
                        principalTable: "cartoes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_categorias_despesa_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias_despesa",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_despesas_recorrentes_id_despesa_recorrente",
                        column: x => x.id_despesa_recorrente,
                        principalTable: "despesas_recorrentes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_despesas_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_beneficiario",
                table: "despesas",
                column: "id_beneficiario");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_cartao",
                table: "despesas",
                column: "id_cartao");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_categoria",
                table: "despesas",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_despesa_recorrente",
                table: "despesas",
                column: "id_despesa_recorrente");

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_despesa_recorrente_mes_referencia",
                table: "despesas",
                columns: new[] { "id_despesa_recorrente", "mes_referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_despesas_id_usuario",
                table: "despesas",
                column: "id_usuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "despesas");
        }
    }
}
