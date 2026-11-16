// Diz quais CLAUDE.md de pasta valem para um conjunto de arquivos. O Claude Code carrega o CLAUDE.md
// de uma pasta quando lê um arquivo dela, mas não se sabe se isso vale para um subagente nem para o
// @import dentro de um CLAUDE.md de pasta. Por isso os revisores, o autor de testes, o planejador e a
// implementar-issue rodam este script sobre os arquivos do diff (ou da tabela de Arquivos do plano) e
// leem o que ele devolve.
//
// Uso: node scripts/harness/claude-md-aplicaveis.mjs <arquivo>...   (ou a lista no stdin, um por linha)
// Saída: um caminho por linha, relativo à raiz do repositório, na ordem de leitura: de fora para
// dentro, e o arquivo importado antes de quem o importa. O CLAUDE.md da raiz fica de fora, porque
// ele já está no contexto de toda sessão.

import { existsSync, readFileSync, statSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const RAIZ_REPOSITORIO = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

/**
 * Os @imports de um CLAUDE.md: um `@caminho.md` no começo da linha ou depois de um espaço, fora de
 * bloco de código e de trecho entre crases. O caminho é relativo ao arquivo que importa.
 * @param {string} conteudo
 * @returns {string[]}
 */
export function importsDe(conteudo) {
  const imports = [];
  let dentroBloco = false;
  for (const linha of conteudo.split(/\r?\n/)) {
    if (/^\s*(```|~~~)/.test(linha)) {
      dentroBloco = !dentroBloco;
      continue;
    }
    if (dentroBloco) continue;
    const semCodigo = linha.replace(/`[^`]*`/g, "");
    for (const [, alvo] of semCodigo.matchAll(/(?:^|\s)@([^\s`)]+\.md)\b/g)) imports.push(alvo);
  }
  return imports;
}

/**
 * @param {string[]} arquivos caminhos relativos à raiz (ou absolutos dentro dela)
 * @param {string} raiz raiz do repositório
 * @returns {string[]} os CLAUDE.md que valem, relativos à raiz, com barras normais
 */
export function claudeMdAplicaveis(arquivos, raiz = RAIZ_REPOSITORIO) {
  const vistos = new Set();
  const ordem = [];

  const visitar = (absoluto) => {
    const relativo = path.relative(raiz, absoluto).replaceAll("\\", "/");
    if (vistos.has(relativo) || relativo === "CLAUDE.md" || relativo.startsWith("..")) return;
    vistos.add(relativo);
    for (const alvo of importsDe(readFileSync(absoluto, "utf8"))) {
      const importado = path.resolve(path.dirname(absoluto), alvo);
      if (existsSync(importado)) visitar(importado);
    }
    ordem.push(relativo);
  };

  const normalizados = arquivos
    .map((bruto) => bruto.trim().replaceAll("\\", "/"))
    .filter(Boolean)
    .sort();

  for (const arquivo of normalizados) {
    const absoluto = path.resolve(raiz, arquivo);
    if (path.relative(raiz, absoluto).startsWith("..")) continue;
    const ehPasta = existsSync(absoluto) && statSync(absoluto).isDirectory();
    const pasta = ehPasta ? absoluto : path.dirname(absoluto);

    const ancestrais = [];
    for (let atual = pasta; path.relative(raiz, atual) !== ""; atual = path.dirname(atual)) {
      ancestrais.unshift(atual);
    }
    for (const ancestral of ancestrais) {
      const claudeMd = path.join(ancestral, "CLAUDE.md");
      if (existsSync(claudeMd)) visitar(claudeMd);
    }
  }

  return ordem;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const argumentos = process.argv.slice(2);
  const arquivos = argumentos.length ? argumentos : readFileSync(0, "utf8").split("\n");
  for (const claudeMd of claudeMdAplicaveis(arquivos)) console.log(claudeMd);
}
