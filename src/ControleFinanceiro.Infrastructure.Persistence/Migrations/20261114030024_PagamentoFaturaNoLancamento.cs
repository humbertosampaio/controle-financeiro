using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PagamentoFaturaNoLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "eh_pagamento_fatura",
                table: "transacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_transacoes_pagamento_fatura",
                table: "transacoes",
                sql: "NOT eh_pagamento_fatura OR (id_cartao IS NOT NULL AND valor >= 0 AND parcela_numero IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_transacoes_pagamento_fatura",
                table: "transacoes");

            migrationBuilder.DropColumn(
                name: "eh_pagamento_fatura",
                table: "transacoes");
        }
    }
}
