using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CartoesDiaFechamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "dia_fechamento",
                table: "cartoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_cartoes_dia_fechamento",
                table: "cartoes",
                sql: "dia_fechamento BETWEEN 1 AND 28");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_cartoes_dia_fechamento",
                table: "cartoes");

            migrationBuilder.DropColumn(
                name: "dia_fechamento",
                table: "cartoes");
        }
    }
}
