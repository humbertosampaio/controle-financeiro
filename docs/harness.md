# Harness de IA

Como este repositório faz agentes de IA trabalharem dentro das regras: o que bloqueia, o que revisa e o que confere a cada commit e PR. O [CLAUDE.md](../CLAUDE.md) diz a **regra**; este documento descreve o **mecanismo**.

## Por que existe

As duas falhas mais caras aqui não aparecem no build:

1. **Dado financeiro real no git.** Um extrato commitado, mesmo apagado depois, continua no histórico.
2. **Secret no git.** Chave da Anthropic ou credencial da Pluggy commitada precisa ser trocada.

Regra que vive só em documento não resiste ao volume de código que um agente produz. Por isso cada regra que dá para verificar mecanicamente tem um mecanismo, e a revisão humana fica para o que não dá.

## As três camadas

| Camada | Quando roda | Bloqueia? | Onde |
|---|---|---|---|
| **Guardrail** | a cada comando ou escrita do agente | sim | `.claude/settings.json` → `scripts/harness/guardrail.mjs` |
| **Agentes** | sob demanda, antes do PR | não, reportam | `.claude/agents/` |
| **Gates** | pre-commit, commit-msg, pre-push, CI | sim | `.githooks/`, `.github/workflows/ci.yml` |

O guardrail evita o erro, os agentes acham o que passou, e os gates garantem que nada chegue à `main` sem passar pelos dois.

## Camada 1: guardrail

Hook `PreToolUse` do Claude Code em `Bash`, `PowerShell`, `Write`, `Edit` e `NotebookEdit`. Para cada chamada, responde uma de três coisas:

- **deny:** o agente não executa e recebe o motivo e a alternativa.
- **ask:** a operação é legítima mas guardada; quem confirma é o humano.
- **allow** (sem saída): segue o fluxo normal de permissão.

| deny | ask |
|---|---|
| `git add` de `dados/` ou de extrato fora de `tests/**/Fixtures/` | push forçado (inclusive `--force-with-lease`) |
| `git add --force` (contorna o `.gitignore`) | `reset --hard`, `clean -f`, apagar ou renomear branch, `rebase` |
| `--no-verify` / `git commit -n` | remoção recursiva de arquivos |
| push na `main`, explícito ou a partir dela | `dotnet add/remove package` |
| `dotnet user-secrets list` (imprime secrets) | `dotnet ef database drop/update`, `migrations remove` |
| secret no comando ou no conteúdo escrito | apagar container ou volume do Docker |
| senha em `appsettings*.json` | escrever migration, `.gitignore`, `Directory.Packages.props` |
| escrever dentro de `dados/` | escrever no próprio harness (`.claude/settings*.json`, `.githooks/`, `scripts/harness/`, CI) |

**deny vence ask:** um secret indo para um arquivo guardado bloqueia, não vira pergunta.

**Falha aberta, de propósito:** erro inesperado no guardrail não bloqueia nada. Um bug nele não pode travar a sessão. Em troca, ele tem testes que cobrem os dois lados: o que tem que ser barrado e o fluxo normal que **não** pode ser barrado. O segundo é o que faz alguém desligar o harness.

```bash
node scripts/harness/guardrail.test.mjs
```

## Camada 2: agentes

Todos respondem em PT-BR e citam `arquivo:linha`. Os revisores só leem.

| Agente | Quando |
|---|---|
| `planejador` | antes do código, em tarefa não trivial |
| `autor-de-testes` | código novo sem teste; escreve, não é só leitura |
| `revisor-dominio` | diff em `src/**`: camadas, agregados, `Result`, Vogen, importação idempotente, migrations |
| `revisor-privacidade` | antes de todo PR: dado real ou secret no repositório, dado demais indo para o modelo |

Os revisores leem o `--stat` antes do diff. Até ~800 linhas, leem tudo; acima disso, vão arquivo por arquivo em ordem de risco e **dizem o que não leram**. Revisão que para no meio sem avisar passa a impressão de que tudo foi revisado.

Cada revisor carrega uma lista de **dívidas conhecidas** que não deve reportar. Revisor que repete os mesmos achados a cada PR passa a ser ignorado.

As skills `planejar-issue`, `revisar-mudanca` e `abrir-pr` encadeiam agentes e gates nas pontas do ciclo de uma issue.

## Camada 3: gates

Ative uma vez por clone:

```bash
git config core.hooksPath .githooks
```

| Gate | O que confere |
|---|---|
| `pre-commit` | `scan-staged.sh`: arquivo de `dados/`, extrato fora de Fixtures, chave da Anthropic, token do GitHub, chave privada, senha em appsettings |
| `commit-msg` | Conventional Commits em PT-BR, descrição com maiúscula e sem ponto final |
| `pre-push` | nada vai para a `main`; branch `<tipo>/<issue>-<slug>`; testes do guardrail; build e testes |
| CI (`ci.yml`) | build e testes; testes do guardrail; varredura dos arquivos do PR |

## Manutenção

- **Regra nova do projeto?** Atualize o agente que a revisa. Se ela é verificável mecanicamente, prefira um gate a um parágrafo.
- **Achado repetido nas revisões?** Vira dívida conhecida no agente (para ele parar de repetir) e uma issue `tipo:tech` (para alguém corrigir).
- **O guardrail bloqueou algo legítimo?** Primeiro acrescente o caso em `guardrail.test.mjs` e veja o teste falhar; depois ajuste o padrão. Nunca contorne com `--no-verify`.
- **A IA errou a mesma coisa duas vezes?** Vira antipadrão em [trabalhando-com-ia.md](trabalhando-com-ia.md#antipadrões) e, se der, uma regra no guardrail.
