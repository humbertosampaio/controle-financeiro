using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ControleFinanceiro.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CategoriasDespesa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_despesa",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_despesa", x => x.id);
                    table.CheckConstraint("CK_categorias_despesa_nome", "btrim(nome) <> ''");
                    table.ForeignKey(
                        name: "FK_categorias_despesa_usuarios_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aliases_categorias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    texto_original = table.Column<string>(type: "text", nullable: false),
                    texto_normalizado = table.Column<string>(type: "text", nullable: false),
                    id_usuario = table.Column<Guid>(type: "uuid", nullable: false),
                    id_categoria = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aliases_categorias", x => x.id);
                    table.CheckConstraint("CK_aliases_categorias_texto_normalizado", "btrim(texto_normalizado) <> ''");
                    table.ForeignKey(
                        name: "FK_aliases_categorias_categorias_despesa_id_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categorias_despesa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_aliases_categorias_id_categoria",
                table: "aliases_categorias",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_despesa_id_usuario",
                table: "categorias_despesa",
                column: "id_usuario");

            // Por SQL porque o EF não cria índice com colunas do alias e do owned que o contém (CategoriaDespesaConfiguration).
            // Um alias aponta para uma categoria só em cada usuário.
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "IX_aliases_categorias_id_usuario_texto_normalizado"
                    ON aliases_categorias (id_usuario, texto_normalizado);
                """);

            // A categoria Outros (CategoriaDespesa.NomeOutros) dos usuários que já existem; os novos a ganham no cadastro.
            // O nome fica fixo aqui: a migration grava o que valia quando foi escrita.
            migrationBuilder.Sql("""
                INSERT INTO categorias_despesa (id, id_usuario, nome)
                SELECT uuidv7(), id, 'Outros' FROM usuarios;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aliases_categorias");

            migrationBuilder.DropTable(
                name: "categorias_despesa");
        }
    }
}
