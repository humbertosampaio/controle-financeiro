// Testes do script que diz quais CLAUDE.md de pasta valem para os arquivos de um diff. Montam uma
// árvore de pastas temporária, para não depender dos CLAUDE.md que o repositório tem hoje.
//
// node scripts/harness/claude-md-aplicaveis.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { claudeMdAplicaveis, importsDe } from "./claude-md-aplicaveis.mjs";

function montarArvore(arquivos) {
  const raiz = mkdtempSync(path.join(tmpdir(), "claude-md-"));
  for (const [caminho, conteudo] of Object.entries(arquivos)) {
    const absoluto = path.join(raiz, caminho);
    mkdirSync(path.dirname(absoluto), { recursive: true });
    writeFileSync(absoluto, conteudo);
  }
  return raiz;
}

const arvore = {
  "CLAUDE.md": "raiz",
  "src/CLAUDE.md": "convenções",
  "src/Dominio/CLAUDE.md": "domínio",
  "src/Dominio/Contas/Conta.cs": "",
  "src/Mcp/CLAUDE.md": "@../IA/CLAUDE.md\n",
  "src/Mcp/Program.cs": "",
  "src/IA/CLAUDE.md": "@../CLAUDE.md\n",
  "src/SemClaudeMd/Arquivo.cs": "",
  "tests/CLAUDE.md": "@../src/CLAUDE.md\n\nLeia o guia.",
  "tests/Dominio.Tests/ContaTests.cs": "",
  "docs/guia.md": "",
};

test("arquivos → CLAUDE.md que valem, de fora para dentro e sem a raiz", () => {
  const raiz = montarArvore(arvore);
  try {
    const casos = [
      [["src/Dominio/Contas/Conta.cs"], ["src/CLAUDE.md", "src/Dominio/CLAUDE.md"]],
      [["src\\Dominio\\Contas\\Conta.cs"], ["src/CLAUDE.md", "src/Dominio/CLAUDE.md"]],
      // pasta sem CLAUDE.md próprio: só o ancestral
      [["src/SemClaudeMd/Arquivo.cs"], ["src/CLAUDE.md"]],
      // a própria pasta como argumento
      [["src/Dominio"], ["src/CLAUDE.md", "src/Dominio/CLAUDE.md"]],
      // o importado vem antes de quem importa, e o import encadeado também é seguido
      [["src/Mcp/Program.cs"], ["src/CLAUDE.md", "src/IA/CLAUDE.md", "src/Mcp/CLAUDE.md"]],
      [["tests/Dominio.Tests/ContaTests.cs"], ["src/CLAUDE.md", "tests/CLAUDE.md"]],
      // vários arquivos: sem repetição
      [["src/Dominio/Contas/Conta.cs", "tests/Dominio.Tests/ContaTests.cs", "src/Dominio/Outra.cs"],
        ["src/CLAUDE.md", "src/Dominio/CLAUDE.md", "tests/CLAUDE.md"]],
      // fora de qualquer CLAUDE.md de pasta, ou fora do repositório: nada
      [["docs/guia.md", "CLAUDE.md"], []],
      [["../fora/Arquivo.cs"], []],
      [[], []],
      [["", "  "], []],
    ];
    for (const [arquivos, esperado] of casos) {
      assert.deepEqual(claudeMdAplicaveis(arquivos, raiz), esperado, arquivos.join(", "));
    }
  } finally {
    rmSync(raiz, { recursive: true, force: true });
  }
});

test("import que aponta para arquivo que não existe é ignorado", () => {
  const raiz = montarArvore({ "src/CLAUDE.md": "@inexistente.md\n", "src/A.cs": "" });
  try {
    assert.deepEqual(claudeMdAplicaveis(["src/A.cs"], raiz), ["src/CLAUDE.md"]);
  } finally {
    rmSync(raiz, { recursive: true, force: true });
  }
});

test("só conta como import o @ fora de código", () => {
  const conteudo = [
    "@../src/CLAUDE.md",
    "Leia também @docs/guia.md antes.",
    "Um e-mail fulano@exemplo.md não é import.",
    "Entre crases, `@nao/conta.md` não é import.",
    "```",
    "@nem/aqui.md",
    "```",
  ].join("\n");
  assert.deepEqual(importsDe(conteudo), ["../src/CLAUDE.md", "docs/guia.md"]);
});

test("a linha de comando aceita os arquivos no stdin", () => {
  const script = fileURLToPath(new URL("./claude-md-aplicaveis.mjs", import.meta.url));
  const saida = execFileSync("node", [script], { input: "docs/harness.md\n", encoding: "utf8" });
  assert.equal(saida, "");
});
