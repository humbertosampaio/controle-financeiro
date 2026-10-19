using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VariosCartoesPorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cartao_dia_vencimento",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "cartao_sufixo",
                table: "usuarios");

            migrationBuilder.CreateTable(
                name: "cartoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    banco = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    sufixo = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    dia_vencimento = table.Column<int>(type: "integer", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartoes", x => x.id);
                    table.CheckConstraint("CK_cartoes_banco", "banco ~ '^[0-9]{3}$'");
                    table.CheckConstraint("CK_cartoes_dia_vencimento", "dia_vencimento BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_cartoes_sufixo", "sufixo ~ '^[0-9]{4}$'");
                    table.ForeignKey(
                        name: "FK_cartoes_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cartoes_id_usuario_banco_sufixo",
                table: "cartoes",
                columns: new[] { "id_usuario", "banco", "sufixo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cartoes");

            migrationBuilder.AddColumn<int>(
                name: "cartao_dia_vencimento",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "cartao_sufixo",
                table: "usuarios",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
