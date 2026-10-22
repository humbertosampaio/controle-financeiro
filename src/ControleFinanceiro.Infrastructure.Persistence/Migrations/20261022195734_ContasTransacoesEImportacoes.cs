using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContasTransacoesEImportacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    banco = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    sufixo = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    apelido = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas", x => x.id);
                    table.CheckConstraint("CK_contas_banco", "banco ~ '^[0-9]{3}$'");
                    table.CheckConstraint("CK_contas_sufixo", "sufixo ~ '^[0-9]{3}[0-9X]$'");
                    table.ForeignKey(
                        name: "FK_contas_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "importacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    formato = table.Column<string>(type: "text", nullable: false),
                    lidas = table.Column<int>(type: "integer", nullable: false),
                    importada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    id_cartao = table.Column<Guid>(type: "uuid", nullable: true),
                    id_conta = table.Column<Guid>(type: "uuid", nullable: true),
                    periodo_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_inicio = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacoes", x => x.id);
                    table.CheckConstraint("CK_importacoes_lidas", "lidas >= 0");
                    table.CheckConstraint("CK_importacoes_origem", "num_nonnulls(id_conta, id_cartao) = 1");
                    table.CheckConstraint("CK_importacoes_periodo", "periodo_inicio <= periodo_fim");
                    table.ForeignKey(
                        name: "FK_importacoes_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transacoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_importacao = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: false),
                    contraparte = table.Column<string>(type: "text", nullable: true),
                    chave_deduplicacao = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    id_cartao = table.Column<Guid>(type: "uuid", nullable: true),
                    id_conta = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transacoes", x => x.id);
                    table.CheckConstraint("CK_transacoes_chave_deduplicacao", "chave_deduplicacao ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("CK_transacoes_origem", "num_nonnulls(id_conta, id_cartao) = 1");
                    table.ForeignKey(
                        name: "FK_transacoes_importacoes_id_importacao",
                        column: x => x.id_importacao,
                        principalTable: "importacoes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_transacoes_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contas_id_usuario_banco_sufixo",
                table: "contas",
                columns: new[] { "id_usuario", "banco", "sufixo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_importacoes_id_usuario",
                table: "importacoes",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_chave_deduplicacao",
                table: "transacoes",
                column: "chave_deduplicacao",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_id_importacao",
                table: "transacoes",
                column: "id_importacao");

            migrationBuilder.CreateIndex(
                name: "IX_transacoes_id_usuario",
                table: "transacoes",
                column: "id_usuario");

            // O EF não aceita complex type em FK nem em índice. As FKs da origem são adiadas para o commit: sem elas no
            // modelo, o EF não ordena o insert da conta nova antes das transações e da importação no mesmo SaveChanges.
            // NO ACTION, e não RESTRICT, porque o Postgres não adia o RESTRICT.
            migrationBuilder.Sql("""
                ALTER TABLE transacoes
                    ADD CONSTRAINT "FK_transacoes_contas_id_conta" FOREIGN KEY (id_conta) REFERENCES contas (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE transacoes
                    ADD CONSTRAINT "FK_transacoes_cartoes_id_cartao" FOREIGN KEY (id_cartao) REFERENCES cartoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE importacoes
                    ADD CONSTRAINT "FK_importacoes_contas_id_conta" FOREIGN KEY (id_conta) REFERENCES contas (id)
                    DEFERRABLE INITIALLY DEFERRED;

                ALTER TABLE importacoes
                    ADD CONSTRAINT "FK_importacoes_cartoes_id_cartao" FOREIGN KEY (id_cartao) REFERENCES cartoes (id)
                    DEFERRABLE INITIALLY DEFERRED;

                CREATE INDEX "IX_transacoes_id_conta_data" ON transacoes (id_conta, data);

                CREATE INDEX "IX_transacoes_id_cartao_data" ON transacoes (id_cartao, data);

                CREATE INDEX "IX_importacoes_id_conta_periodo_fim" ON importacoes (id_conta, periodo_fim);

                CREATE INDEX "IX_importacoes_id_cartao_periodo_fim" ON importacoes (id_cartao, periodo_fim);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // As FKs e os índices que o Up criou por SQL saem antes das tabelas: a FK para contas impede o DropTable.
            migrationBuilder.Sql("""
                DROP INDEX "IX_importacoes_id_cartao_periodo_fim";
                DROP INDEX "IX_importacoes_id_conta_periodo_fim";
                DROP INDEX "IX_transacoes_id_cartao_data";
                DROP INDEX "IX_transacoes_id_conta_data";

                ALTER TABLE importacoes DROP CONSTRAINT "FK_importacoes_cartoes_id_cartao";
                ALTER TABLE importacoes DROP CONSTRAINT "FK_importacoes_contas_id_conta";
                ALTER TABLE transacoes DROP CONSTRAINT "FK_transacoes_cartoes_id_cartao";
                ALTER TABLE transacoes DROP CONSTRAINT "FK_transacoes_contas_id_conta";
                """);

            migrationBuilder.DropTable(
                name: "contas");

            migrationBuilder.DropTable(
                name: "transacoes");

            migrationBuilder.DropTable(
                name: "importacoes");
        }
    }
}
