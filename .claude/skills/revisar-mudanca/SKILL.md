---
name: revisar-mudanca
description: Roda a revisão completa da branch do controle-financeiro antes do PR — gates mecânicos (build, testes, guardrail, varredura de secrets e de dados) e os agentes revisores pertinentes ao diff. Use quando a implementação está pronta, antes de abrir-pr.
---

# revisar-mudanca

Gates mecânicos primeiro (baratos e objetivos), agentes depois (caros e contextuais). Não adianta pedir revisão de código que não compila.

## 1. O que mudou

```bash
git fetch origin main --quiet
git diff origin/main...HEAD --stat
```

Decida os revisores pelo diff:

| O diff toca | Agente |
|---|---|
| `src/**` | `revisor-dominio` |
| `src/**` ou `tests/**` | `revisor-convencoes` |
| sempre (é barato rodar a mais e caro pular) | `revisor-privacidade` |
| código novo sem teste correspondente | `autor-de-testes` |

## 2. Gates mecânicos

Rode todos, inclusive os que "com certeza passam":

```bash
dotnet build ControleFinanceiro.slnx
dotnet test ControleFinanceiro.slnx --no-build --filter "FullyQualifiedName!~ControleFinanceiro.WebApi.Tests"
filtro=$(git diff --name-only origin/main...HEAD | node scripts/harness/testes-afetados.mjs); echo "integração: ${filtro:-nenhum}"
node scripts/harness/guardrail.test.mjs
node scripts/harness/testes-afetados.test.mjs
sh .githooks/scan-staged.sh --branch origin/main
```

Depois, os testes de integração afetados (precisam do Docker), conforme o `filtro`:

- vazio: nenhum;
- `todos`: `dotnet test tests/ControleFinanceiro.WebApi.Tests --no-build`;
- outro valor: `dotnet test tests/ControleFinanceiro.WebApi.Tests --no-build --filter "$filtro"`.

O último comando da lista varre os arquivos alterados na branch em busca de secret e de dado financeiro.

Se o diff toca `src/ControleFinanceiro.Domain/**` ou `src/ControleFinanceiro.Infrastructure.Persistence/**`, confira se o modelo do EF mudou sem migration:

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/ControleFinanceiro.Infrastructure.Persistence
```

Saída diferente de "No changes have been made to the model since the last migration." (o comando sai com código 1) é vermelho: gere a migration ou desfaça a mudança no mapeamento. Sem essa conferência, o problema só aparece nos testes de integração, como falha no `MigrateAsync` que não aponta a causa.

Algum vermelho: **pare e corrija antes de seguir.**

## 3. Checagens rápidas no diff

```bash
git diff origin/main...HEAD -- src/ControleFinanceiro.Domain | grep -nE '^\+.*using (Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore|Npgsql)'
git diff origin/main...HEAD | grep -nE '^\+.*(TODO|FIXME|Console\.Write)'
git diff origin/main...HEAD --name-only | grep -E '\.(ofx|csv|xlsx)$' | grep -v '/Fixtures/'
git log origin/main..HEAD --format='%s' | grep -vE '^(feat|fix|chore|docs|refactor|test|perf|build|ci)(\([a-z0-9-]+\))?!?: [A-ZÁÉÍÓÚÂÊÔÃÕÇ]'
```

Qualquer saída é achado.

## 4. Agentes

Rode os revisores escolhidos no passo 1, em paralelo. Se o `autor-de-testes` escrever testes, rode o passo 2 de novo.

## 5. Relatório

```
## Gates
build ✅ | unidade ✅ (N) | integração ✅ (N, filtro) | modelo do EF ✅ (ou "não se aplica") | guardrail ✅ | varredura ✅

## Revisores
<achados agrupados por severidade, com arquivo:linha>

## Não verificado
<o que nenhum gate ou revisor cobriu>
```

Todo achado, de qualquer severidade, é corrigido antes do `abrir-pr`, e a revisão roda de novo. Achado que você não pretende atender (mesmo BAIXO, MÉDIO ou contrário a uma decisão fechada no plano): pare, explique ao dono o que o revisor apontou e por que você o ignoraria, e espere a decisão dele. Só ele decide ignorar; não basta citar o achado no PR ou no relato final.
