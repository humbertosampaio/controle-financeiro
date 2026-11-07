using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PagamentoNoMes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_mes",
                table: "parcelas_despesa",
                sql: "mes_referencia = date_trunc('month', data_pagamento_projetada::timestamp)::date");

            migrationBuilder.AddCheckConstraint(
                name: "CK_despesas_pagamento_mes",
                table: "despesas",
                sql: "mes_referencia = date_trunc('month', data_pagamento_projetada::timestamp)::date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_parcelas_despesa_pagamento_mes",
                table: "parcelas_despesa");

            migrationBuilder.DropCheckConstraint(
                name: "CK_despesas_pagamento_mes",
                table: "despesas");
        }
    }
}
