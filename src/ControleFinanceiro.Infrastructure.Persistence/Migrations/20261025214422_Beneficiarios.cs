using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Beneficiarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beneficiarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beneficiarios", x => x.id);
                    table.CheckConstraint("CK_beneficiarios_nome", "btrim(nome) <> ''");
                    table.ForeignKey(
                        name: "FK_beneficiarios_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aliases_beneficiarios",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    texto_original = table.Column<string>(type: "text", nullable: false),
                    texto_normalizado = table.Column<string>(type: "text", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_beneficiario = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aliases_beneficiarios", x => x.id);
                    table.CheckConstraint("CK_aliases_beneficiarios_texto_normalizado", "btrim(texto_normalizado) <> ''");
                    table.ForeignKey(
                        name: "FK_aliases_beneficiarios_beneficiarios_id_beneficiario",
                        column: x => x.id_beneficiario,
                        principalTable: "beneficiarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aliases_beneficiarios_id_beneficiario",
                table: "aliases_beneficiarios",
                column: "id_beneficiario");

            migrationBuilder.CreateIndex(
                name: "IX_beneficiarios_id_usuario",
                table: "beneficiarios",
                column: "id_usuario");

            // Por SQL porque o EF não cria índice com colunas do alias e do owned que o contém (BeneficiarioConfiguration).
            // Um alias aponta para um beneficiário só em cada usuário.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_aliases_beneficiarios_id_usuario_texto_normalizado"
                    ON aliases_beneficiarios (id_usuario, texto_normalizado);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aliases_beneficiarios");

            migrationBuilder.DropTable(
                name: "beneficiarios");
        }
    }
}
