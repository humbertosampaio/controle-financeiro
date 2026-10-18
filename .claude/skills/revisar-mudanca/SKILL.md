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
| sempre (é barato rodar a mais e caro pular) | `revisor-privacidade` |
| código novo sem teste correspondente | `autor-de-testes` |

## 2. Gates mecânicos

Rode todos, inclusive os que "com certeza passam":

```bash
dotnet build ControleFinanceiro.slnx
dotnet test ControleFinanceiro.slnx
node scripts/harness/guardrail.test.mjs
sh .githooks/scan-staged.sh --branch origin/main
```

O último varre os arquivos alterados na branch em busca de secret e de dado financeiro.

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
build ✅ | testes ✅ (N) | guardrail ✅ | varredura ✅

## Revisores
<achados agrupados por severidade, com arquivo:linha>

## Não verificado
<o que nenhum gate ou revisor cobriu>
```

Achado CRÍTICO ou ALTO: corrija e rode a revisão de novo antes do `abrir-pr`.
