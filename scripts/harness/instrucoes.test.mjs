// Confere as instruções do repositório: o tamanho dos CLAUDE.md, para que cada regra nova tenha de
// justificar o lugar, e que os links, as âncoras, os @imports e os caminhos citados nos CLAUDE.md, em
// docs/, nos agentes e nas skills continuem apontando para algo que existe. Roda sobre os arquivos do
// git (git ls-files), no CI e no pre-push.
//
// node scripts/harness/instrucoes.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { claudeMdAplicaveis, importsDe } from "./claude-md-aplicaveis.mjs";

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const KB = 1024;
const TETO_RAIZ = 12 * KB;
const TETO_SRC = 12 * KB;
const TETO_PASTA = 4 * KB;
const TETO_CADEIA = 24 * KB;
const AVISO = "Revise as instruções da cadeia que estourou (docs/harness.md, \"Revisão das instruções\").";

// Caminho entre crases conferido: começa por uma destas pastas.
const PREFIXOS_CAMINHO = ["src/", "tests/", "docs/", "scripts/", ".claude/", ".githooks/", ".github/"];

const rastreados = execFileSync("git", ["ls-files"], { cwd: RAIZ, encoding: "utf8" })
  .split("\n")
  .filter(Boolean);
const existentes = new Set(rastreados);
for (const arquivo of rastreados) {
  for (let pasta = path.posix.dirname(arquivo); pasta !== "."; pasta = path.posix.dirname(pasta)) {
    existentes.add(pasta);
  }
}

const claudeMds = rastreados.filter((arquivo) => path.posix.basename(arquivo) === "CLAUDE.md");
const instrucoes = rastreados.filter(
  (arquivo) =>
    path.posix.basename(arquivo) === "CLAUDE.md" ||
    (arquivo.startsWith("docs/") && arquivo.endsWith(".md")) ||
    /^\.claude\/agents\/[^/]+\.md$/.test(arquivo) ||
    /^\.claude\/skills\/[^/]+\/SKILL\.md$/.test(arquivo),
);

const tamanho = (arquivo) => Buffer.byteLength(readFileSync(path.join(RAIZ, arquivo)));
const ler = (arquivo) => readFileSync(path.join(RAIZ, arquivo), "utf8");

/** As linhas fora de bloco de código, com o número de cada uma. */
function linhasForaCodigo(conteudo) {
  const linhas = [];
  let dentroBloco = false;
  conteudo.split(/\r?\n/).forEach((linha, indice) => {
    if (/^\s*(```|~~~)/.test(linha)) {
      dentroBloco = !dentroBloco;
      return;
    }
    if (!dentroBloco) linhas.push({ linha, numero: indice + 1 });
  });
  return linhas;
}

/** A âncora que o GitHub gera para um título. */
export function ancora(titulo) {
  return titulo
    .replace(/`/g, "")
    .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N} _-]/gu, "")
    .replace(/ /g, "-");
}

function ancorasDe(conteudo) {
  const vistas = new Map();
  const ancoras = new Set();
  for (const { linha } of linhasForaCodigo(conteudo)) {
    const titulo = /^#{1,6}\s+(.*?)\s*#*\s*$/.exec(linha);
    if (!titulo) continue;
    const base = ancora(titulo[1]);
    const repeticoes = vistas.get(base) ?? 0;
    vistas.set(base, repeticoes + 1);
    ancoras.add(repeticoes ? `${base}-${repeticoes}` : base);
  }
  return ancoras;
}

/** Os links Markdown locais: [texto](alvo), fora de código. */
export function linksDe(conteudo) {
  const links = [];
  for (const { linha, numero } of linhasForaCodigo(conteudo)) {
    const semCodigo = linha.replace(/`[^`]*`/g, "");
    for (const [, alvo] of semCodigo.matchAll(/\]\(([^)\s]+)\)/g)) {
      if (/^[a-z][a-z0-9+.-]*:/i.test(alvo)) continue; // http:, mailto:...
      links.push({ alvo, numero });
    }
  }
  return links;
}

/** Os caminhos do repositório citados entre crases, fora de bloco de código. */
export function caminhosDe(conteudo) {
  const caminhos = [];
  for (const { linha, numero } of linhasForaCodigo(conteudo)) {
    for (const [, trecho] of linha.matchAll(/`([^`]+)`/g)) {
      if (!PREFIXOS_CAMINHO.some((prefixo) => trecho.startsWith(prefixo))) continue;
      if (/[\s*<>{}]|\.\.\./.test(trecho)) continue; // comando, curinga ou modelo de caminho
      caminhos.push({ caminho: trecho.replace(/[/:]$/, "").replace(/:\d+$/, ""), numero });
    }
  }
  return caminhos;
}

test("CLAUDE.md dentro do teto de tamanho", () => {
  const estourados = [];
  for (const arquivo of claudeMds) {
    const teto = arquivo === "CLAUDE.md" ? TETO_RAIZ : arquivo === "src/CLAUDE.md" ? TETO_SRC : TETO_PASTA;
    const bytes = tamanho(arquivo);
    if (bytes > teto) estourados.push(`${arquivo}: ${bytes} bytes, teto ${teto}`);
  }
  assert.deepEqual(estourados, [], `${estourados.join("\n")}\n${AVISO}`);
});

test("cadeia efetiva de cada pasta dentro do teto", () => {
  const estouradas = [];
  for (const arquivo of claudeMds.filter((claudeMd) => claudeMd !== "CLAUDE.md")) {
    const cadeia = ["CLAUDE.md", ...claudeMdAplicaveis([path.posix.dirname(arquivo)], RAIZ)];
    const bytes = cadeia.reduce((soma, claudeMd) => soma + tamanho(claudeMd), 0);
    if (bytes > TETO_CADEIA) estouradas.push(`${cadeia.join(" + ")}: ${bytes} bytes, teto ${TETO_CADEIA}`);
  }
  assert.deepEqual(estouradas, [], `${estouradas.join("\n")}\n${AVISO}`);
});

test("@imports dos CLAUDE.md apontam para arquivos do repositório", () => {
  const quebrados = [];
  for (const arquivo of claudeMds) {
    for (const alvo of importsDe(ler(arquivo))) {
      const resolvido = path.posix.normalize(path.posix.join(path.posix.dirname(arquivo), alvo));
      if (!existentes.has(resolvido)) quebrados.push(`${arquivo}: @${alvo}`);
    }
  }
  assert.deepEqual(quebrados, []);
});

test("links e âncoras Markdown das instruções resolvem", () => {
  const ancorasPorArquivo = new Map();
  const ancorasEm = (arquivo) => {
    if (!ancorasPorArquivo.has(arquivo)) ancorasPorArquivo.set(arquivo, ancorasDe(ler(arquivo)));
    return ancorasPorArquivo.get(arquivo);
  };

  const quebrados = [];
  for (const arquivo of instrucoes) {
    for (const { alvo, numero } of linksDe(ler(arquivo))) {
      const [caminhoBruto, ancoraBruta] = decodeURIComponent(alvo).split("#");
      const destino = caminhoBruto
        ? path.posix.normalize(path.posix.join(path.posix.dirname(arquivo), caminhoBruto)).replace(/\/$/, "")
        : arquivo;
      if (!existentes.has(destino)) {
        quebrados.push(`${arquivo}:${numero}: ${alvo} (arquivo não existe)`);
      } else if (ancoraBruta && destino.endsWith(".md") && !ancorasEm(destino).has(ancoraBruta.toLowerCase())) {
        quebrados.push(`${arquivo}:${numero}: ${alvo} (âncora não existe)`);
      }
    }
  }
  assert.deepEqual(quebrados, []);
});

test("caminhos entre crases das instruções existem", () => {
  const quebrados = [];
  for (const arquivo of instrucoes) {
    for (const { caminho, numero } of caminhosDe(ler(arquivo))) {
      if (!existentes.has(caminho)) quebrados.push(`${arquivo}:${numero}: ${caminho}`);
    }
  }
  assert.deepEqual(quebrados, []);
});

test("âncora segue a regra do GitHub", () => {
  assert.equal(ancora("7. Value objects e ids"), "7-value-objects-e-ids");
  assert.equal(ancora("Refino contínuo"), "refino-contínuo");
  assert.equal(ancora("Orquestração com `Result` (diretriz, não regra)"), "orquestração-com-result-diretriz-não-regra");
});

test("só conta como caminho o que começa por uma pasta do repositório", () => {
  const conteudo = [
    "Veja `src/CLAUDE.md` e `docs/harness.md:12`, mas não `Application/IA/Saida`.",
    "Modelos não contam: `tests/**/Fixtures/`, `src/X/<Area>/` e `dotnet test tests/A`.",
    "```",
    "`src/nao/conta.cs`",
    "```",
  ].join("\n");
  assert.deepEqual(caminhosDe(conteudo).map((c) => c.caminho), ["src/CLAUDE.md", "docs/harness.md"]);
});

test("link externo não é conferido", () => {
  const conteudo = "[a](https://exemplo.com) [b](../docs/x.md#y) `[c](nao.md)`";
  assert.deepEqual(linksDe(conteudo).map((l) => l.alvo), ["../docs/x.md#y"]);
});
