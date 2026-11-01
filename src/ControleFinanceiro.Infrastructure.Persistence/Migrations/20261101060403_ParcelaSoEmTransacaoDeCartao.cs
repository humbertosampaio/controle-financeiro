using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParcelaSoEmTransacaoDeCartao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes",
                sql: "num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total AND id_cartao IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_parcela",
                table: "transacoes",
                sql: "num_nulls(parcela_numero, parcela_total) = 2 OR (num_nulls(parcela_numero, parcela_total) = 0 AND parcela_total >= 2 AND parcela_numero BETWEEN 1 AND parcela_total)");
        }
    }
}
