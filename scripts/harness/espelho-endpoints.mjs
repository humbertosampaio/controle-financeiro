// Confere o espelho dos testes de integração: cada pasta de endpoint,
// src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/, tem a pasta
// tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/ com pelo menos um *Tests.cs. A pasta conta como de
// endpoint quando tem um *Endpoint.cs; a que não tem fica de fora. Um *Endpoint.cs fora de <Area>/<Acao>/ também
// é apontado, porque o espelho e os testes afetados do pre-push não o alcançam. O espelho-endpoints.test.mjs confere o
// repositório (git ls-files), no CI e no pre-push.

const ENDPOINTS = "src/ControleFinanceiro.WebApi/Endpoints/";
const TESTES = "tests/ControleFinanceiro.WebApi.Tests/";

/**
 * @param {string[]} arquivos caminhos relativos à raiz do repositório
 * @returns {string[]} as pastas <Area>/<Acao> de endpoint sem teste espelhado e os *Endpoint.cs fora de uma, em ordem
 */
export function espelhosFaltando(arquivos) {
  const normalizados = arquivos.map((bruto) => bruto.trim().replaceAll("\\", "/")).filter(Boolean);

  const pastasEndpoint = new Set();
  const pastasTestadas = new Set();
  const foraEspelho = [];
  for (const arquivo of normalizados) {
    if (arquivo.startsWith(ENDPOINTS) && arquivo.endsWith("Endpoint.cs")) {
      const endpoint = acaoDe(arquivo, ENDPOINTS);
      if (endpoint) pastasEndpoint.add(endpoint);
      else foraEspelho.push(arquivo.slice(ENDPOINTS.length));
    }

    const teste = acaoDe(arquivo, TESTES);
    if (teste && arquivo.endsWith("Tests.cs")) pastasTestadas.add(teste);
  }

  return [...pastasEndpoint].filter((pasta) => !pastasTestadas.has(pasta)).concat(foraEspelho).sort();
}

/** A pasta <Area>/<Acao> do arquivo que está direto nela, dentro da raiz dada; ou nulo. */
function acaoDe(arquivo, raiz) {
  if (!arquivo.startsWith(raiz)) return null;
  const partes = arquivo.slice(raiz.length).split("/");
  return partes.length === 3 ? `${partes[0]}/${partes[1]}` : null;
}
