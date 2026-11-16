// Testes do script que confere o espelho dos testes de integração dos endpoints, e o próprio espelho no
// repositório. Roda no CI e no pre-push.
//
// node scripts/harness/espelho-endpoints.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { espelhosFaltando } from "./espelho-endpoints.mjs";

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const E = "src/ControleFinanceiro.WebApi/Endpoints";
const T = "tests/ControleFinanceiro.WebApi.Tests";

test("endpoint com teste espelhado passa", () => {
  assert.deepEqual(
    espelhosFaltando([
      `${E}/Usuarios/Cadastrar/CadastrarUsuarioEndpoint.cs`,
      `${E}/Usuarios/Cadastrar/CadastrarUsuarioRequest.cs`,
      `${T}/Usuarios/Cadastrar/CadastrarUsuarioEndpointTests.cs`,
    ]),
    [],
  );
});

test("endpoint sem teste espelhado é apontado", () => {
  assert.deepEqual(
    espelhosFaltando([
      `${E}/Usuarios/Cadastrar/CadastrarUsuarioEndpoint.cs`,
      `${E}/Despesas/Vincular/VincularDespesaEndpoint.cs`,
      `${T}/Usuarios/Cadastrar/CadastrarUsuarioEndpointTests.cs`,
    ]),
    ["Despesas/Vincular"],
  );
});

test("pasta espelhada sem *Tests.cs não conta", () => {
  assert.deepEqual(
    espelhosFaltando([
      `${E}/Usuarios/Cadastrar/CadastrarUsuarioEndpoint.cs`,
      `${T}/Usuarios/Cadastrar/CadastrarUsuarioRequestCustomization.cs`,
    ]),
    ["Usuarios/Cadastrar"],
  );
});

test("teste em outra pasta não espelha", () => {
  assert.deepEqual(
    espelhosFaltando([
      `${E}/Usuarios/Cadastrar/CadastrarUsuarioEndpoint.cs`,
      `${T}/Usuarios/CadastrarUsuarioEndpointTests.cs`,
      `${T}/Usuarios/Cadastrar/Sub/CadastrarUsuarioEndpointTests.cs`,
    ]),
    ["Usuarios/Cadastrar"],
  );
});

test("pasta sem *Endpoint.cs fica de fora", () => {
  assert.deepEqual(
    espelhosFaltando([`${E}/Usuarios/Compartilhado/UsuarioResponse.cs`, `${E}/Usuarios/Erros.cs`]),
    [],
  );
});

test("endpoint fora de <Area>/<Acao>/ é apontado", () => {
  assert.deepEqual(
    espelhosFaltando([
      `${E}/Usuarios/CadastrarUsuarioEndpoint.cs`,
      `${E}/Usuarios/Cadastrar/Lote/CadastrarLoteEndpoint.cs`,
      `${T}/Usuarios/CadastrarUsuarioEndpointTests.cs`,
    ]),
    ["Usuarios/Cadastrar/Lote/CadastrarLoteEndpoint.cs", "Usuarios/CadastrarUsuarioEndpoint.cs"],
  );
});

test("aceita barra invertida e linhas vazias", () => {
  assert.deepEqual(
    espelhosFaltando([
      "src\\ControleFinanceiro.WebApi\\Endpoints\\Usuarios\\Cadastrar\\CadastrarUsuarioEndpoint.cs",
      "tests\\ControleFinanceiro.WebApi.Tests\\Usuarios\\Cadastrar\\CadastrarUsuarioEndpointTests.cs",
      "",
      "  ",
    ]),
    [],
  );
});

test("o repositório não tem endpoint sem espelho", () => {
  const rastreados = execFileSync("git", ["ls-files"], { cwd: RAIZ, encoding: "utf8" }).split("\n");
  const faltando = espelhosFaltando(rastreados);
  assert.deepEqual(
    faltando,
    [],
    `Endpoint sem teste de integração em ${T}/<Area>/<Acao>/ (docs/testes-com-autofixture.md): ${faltando.join(", ")}`,
  );
});
