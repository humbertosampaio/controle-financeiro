using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParcelaCategoriaBancoTransacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "categoria_banco",
                table: "transacoes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parcela_numero",
                table: "transacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "parcela_total",
                table: "transacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes",
                sql: "num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes");

            migrationBuilder.DropColumn(
                name: "categoria_banco",
                table: "transacoes");

            migrationBuilder.DropColumn(
                name: "parcela_numero",
                table: "transacoes");

            migrationBuilder.DropColumn(
                name: "parcela_total",
                table: "transacoes");
        }
    }
}
