// Testes do script que escolhe os testes de integração do pre-push e da revisar-mudanca.
// Cobrem os dois lados: deixar de rodar um teste afetado e rodar a suíte inteira sem necessidade.
//
// node scripts/harness/testes-afetados.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { filtroDotnet, testesAfetados } from "./testes-afetados.mjs";

const casos = [
  // endpoint: só a pasta espelhada
  [["src/ControleFinanceiro.WebApi/Endpoints/Usuarios/Cadastrar/CadastrarUsuarioValidator.cs"],
    ["ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar"]],
  [["src\\ControleFinanceiro.WebApi\\Endpoints\\Usuarios\\Cadastrar\\CadastrarUsuarioMapper.cs"],
    ["ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar"]],
  [["tests/ControleFinanceiro.WebApi.Tests/Usuarios/Cadastrar/CadastrarUsuarioEndpointTests.cs"],
    ["ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar"]],
  // dois endpoints: os dois, sem repetir
  [[
    "src/ControleFinanceiro.WebApi/Endpoints/Usuarios/Cadastrar/CadastrarUsuarioEndpoint.cs",
    "src/ControleFinanceiro.WebApi/Endpoints/Usuarios/Cadastrar/CadastrarUsuarioRequest.cs",
    "src/ControleFinanceiro.WebApi/Endpoints/Despesas/Vincular/VincularDespesaEndpoint.cs",
  ], ["ControleFinanceiro.WebApi.Tests.Despesas.Vincular", "ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar"]],
  // persistência, pipeline e infraestrutura dos testes: tudo
  [["src/ControleFinanceiro.Infrastructure.Persistence/Migrations/20260323041817_Initial.cs"], "todos"],
  [["src/ControleFinanceiro.Infrastructure.Persistence/Usuarios/UsuarioConfiguration.cs"], "todos"],
  [["src/ControleFinanceiro.WebApi/Program.cs"], "todos"],
  [["src/ControleFinanceiro.AppHost/Resources/postgres.Dockerfile"], "todos"],
  [["tests/ControleFinanceiro.WebApi.Tests/Infraestrutura/ApiComPostgresFixture.cs"], "todos"],
  [["tests/ControleFinanceiro.Tests.Shared/AutoFixture/DomainFixture.cs"], "todos"],
  [["Directory.Packages.props"], "todos"],
  [["src/ControleFinanceiro.WebApi/Endpoints/Usuarios/Cadastrar/X.cs", "src/ControleFinanceiro.Infrastructure.Persistence/X.cs"], "todos"],
  // regra de domínio e validators compartilhados mudam a resposta dos endpoints: tudo
  [["src/ControleFinanceiro.Domain/Shared/ValueObjects/SufixoCartao.cs"], "todos"],
  [["src/ControleFinanceiro.Application/Validation/CustomValidators.cs"], "todos"],
  // o leitor do layout decide o que a importação grava: tudo
  [["src/ControleFinanceiro.Infrastructure.Importacao/C6/LeitorExtratoContaC6.cs"], "todos"],
  // nada de integração: testes de domínio, docs, harness
  [["tests/ControleFinanceiro.Domain.Tests/Usuarios/UsuarioTests.cs"], "nenhum"],
  [["tests/ControleFinanceiro.Infrastructure.Importacao.Tests/C6/LeitorExtratoContaC6Tests.cs"], "nenhum"],
  [["docs/harness.md", "CLAUDE.md", ".githooks/pre-push"], "nenhum"],
  [[], "nenhum"],
  [["", "  "], "nenhum"],
];

test("arquivos alterados → testes de integração", () => {
  for (const [arquivos, esperado] of casos) {
    assert.deepEqual(testesAfetados(arquivos), esperado, arquivos.join(", "));
  }
});

test("filtro do dotnet test não pega ação de nome parecido", () => {
  assert.equal(
    filtroDotnet(["ControleFinanceiro.WebApi.Tests.Despesas.Vincular", "ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar"]),
    "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Despesas.Vincular.|FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar.",
  );
});

test("linha de comando lê argumentos ou stdin", () => {
  const script = "scripts/harness/testes-afetados.mjs";
  const porArgumento = execFileSync("node", [script, "src/ControleFinanceiro.WebApi/Endpoints/Usuarios/Cadastrar/A.cs"], { encoding: "utf8" });
  assert.equal(porArgumento.trim(), "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar.");

  const porStdin = execFileSync("node", [script], { input: "src/ControleFinanceiro.WebApi/Program.cs\n", encoding: "utf8" });
  assert.equal(porStdin.trim(), "todos");

  const nenhum = execFileSync("node", [script], { input: "docs/harness.md\n", encoding: "utf8" });
  assert.equal(nenhum, "");
});
