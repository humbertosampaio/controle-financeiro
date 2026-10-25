using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChecksUsuariosEDescontos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_usuarios_nome",
                table: "usuarios",
                sql: "btrim(nome) <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_usuarios_renda_bruta",
                table: "usuarios",
                sql: "renda_bruta >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_descontos_salariais_descricao",
                table: "descontos_salariais",
                sql: "btrim(descricao) <> ''");

            migrationBuilder.AddCheckConstraint(
                name: "CK_descontos_salariais_valor",
                table: "descontos_salariais",
                sql: "valor >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_usuarios_nome",
                table: "usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_usuarios_renda_bruta",
                table: "usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "CK_descontos_salariais_descricao",
                table: "descontos_salariais");

            migrationBuilder.DropCheckConstraint(
                name: "CK_descontos_salariais_valor",
                table: "descontos_salariais");
        }
    }
}
