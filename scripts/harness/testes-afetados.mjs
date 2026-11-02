// Decide quais testes de integração rodar a partir dos arquivos alterados. Os testes de integração
// espelham as pastas dos endpoints: src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/ é testado
// em tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/, no namespace
// ControleFinanceiro.WebApi.Tests.<Area>.<Acao>.
//
// Uso: node scripts/harness/testes-afetados.mjs <arquivo>...   (ou a lista no stdin, um por linha)
// Saída: vazia (nenhum teste de integração), "todos" ou o valor do --filter do dotnet test.
//
// Com --mcp, responde pela suíte do servidor MCP, que não tem espelho: "todos" ou vazia.

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const NAMESPACE_TESTES = "ControleFinanceiro.WebApi.Tests";

// Pastas espelhadas: a pasta <Area>/<Acao> dentro delas aponta para o mesmo namespace de teste.
// Quando a Application tiver comandos organizados em <Area>/<Acao>, a pasta deles entra aqui.
const espelhadas = [
  "src/ControleFinanceiro.WebApi/Endpoints/",
  "tests/ControleFinanceiro.WebApi.Tests/",
];

// Mudança aqui pode quebrar qualquer endpoint: roda a suíte de integração inteira. Domain e
// Application entram porque as regras dos value objects e os validators compartilhados mudam a
// resposta dos endpoints (um sufixo de cartão que dava 201 passa a dar 400). A Importacao entra
// porque o leitor de cada layout decide o que o endpoint de importação grava.
const afetamTodos = [
  "src/ControleFinanceiro.Domain/",
  "src/ControleFinanceiro.Application/",
  "src/ControleFinanceiro.Infrastructure.Importacao/",
  "src/ControleFinanceiro.Infrastructure.Persistence/", // inclui as migrations
  "src/ControleFinanceiro.WebApi/", // Program.cs e o que não é de um endpoint só
  "src/ControleFinanceiro.AppHost/Resources/", // imagem do Postgres
  "tests/ControleFinanceiro.WebApi.Tests/", // fixture, AutoFixture, csproj
  "tests/ControleFinanceiro.Tests.Shared/",
  "tests/Directory.Build.props",
  "Directory.Packages.props",
];

// Mudança aqui pode quebrar o servidor MCP, que usa o domínio e a persistência, mas não a WebApi nem a Importacao.
const afetamMcp = [
  "src/ControleFinanceiro.Mcp/",
  "tests/ControleFinanceiro.Mcp.Tests/",
  "src/ControleFinanceiro.Domain/",
  "src/ControleFinanceiro.Application/",
  "src/ControleFinanceiro.Infrastructure.Persistence/",
  "src/ControleFinanceiro.AppHost/Resources/", // imagem do Postgres
  "tests/ControleFinanceiro.Tests.Shared/",
  "tests/Directory.Build.props",
  "Directory.Packages.props",
];

/**
 * @param {string[]} arquivos caminhos relativos à raiz do repositório
 * @returns {boolean} se a suíte do servidor MCP precisa rodar
 */
export function mcpAfetado(arquivos) {
  return arquivos
    .map((bruto) => bruto.trim().replaceAll("\\", "/"))
    .some((arquivo) => afetamMcp.some((prefixo) => arquivo.startsWith(prefixo)));
}

/**
 * @param {string[]} arquivos caminhos relativos à raiz do repositório
 * @returns {"nenhum" | "todos" | string[]} os namespaces de teste a rodar
 */
export function testesAfetados(arquivos) {
  const namespaces = new Set();
  let todos = false;

  for (const bruto of arquivos) {
    const arquivo = bruto.trim().replaceAll("\\", "/");
    if (!arquivo) continue;

    const espelho = espelhadas
      .map((pasta) => arquivo.startsWith(pasta) && arquivo.slice(pasta.length).split("/"))
      .find((partes) => partes && partes.length >= 3);
    if (espelho) {
      namespaces.add(`${NAMESPACE_TESTES}.${espelho[0]}.${espelho[1]}`);
      continue;
    }

    if (afetamTodos.some((prefixo) => arquivo.startsWith(prefixo))) todos = true;
  }

  if (todos) return "todos";
  return namespaces.size ? [...namespaces].sort() : "nenhum";
}

/** Valor do --filter do dotnet test para a lista de namespaces. */
export function filtroDotnet(namespaces) {
  // O ponto final evita que Usuarios.Cadastrar pegue também Usuarios.CadastrarEmLote.
  return namespaces.map((ns) => `FullyQualifiedName~${ns}.`).join("|");
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const argumentos = process.argv.slice(2).filter((argumento) => argumento !== "--mcp");
  const arquivos = argumentos.length ? argumentos : readFileSync(0, "utf8").split("\n");
  if (process.argv.includes("--mcp")) {
    if (mcpAfetado(arquivos)) console.log("todos");
  } else {
    const resultado = testesAfetados(arquivos);
    if (resultado === "todos") console.log("todos");
    else if (resultado !== "nenhum") console.log(filtroDotnet(resultado));
  }
}
