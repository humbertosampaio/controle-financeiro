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
| senha em `appsettings*.json` ou `.mcp.json` | escrever migration, `.gitignore`, `Directory.Packages.props` |
| escrever dentro de `dados/` | escrever no próprio harness (`.claude/settings*.json`, `.githooks/`, `scripts/harness/`, CI) |

**deny vence ask:** um secret indo para um arquivo guardado bloqueia, não vira pergunta.

### Ferramentas do servidor MCP que gravam

O guardrail não olha as ferramentas MCP. Quem pede a aprovação do usuário antes de uma ferramenta do `controle-financeiro` gravar são duas travas, e uma ferramenta que grava (`ReadOnly = false`) precisa das duas:

- **a meta `anthropic/requiresUserInteraction`** na própria ferramenta (`[McpMeta(...)]`), que o Claude Code no terminal honra em todos os modos, inclusive no auto e no de ignorar permissões;
- **uma regra `ask`** em `permissions` do `.claude/settings.json`, como `mcp__controle-financeiro__confirmar_vinculo`. O Claude Code do app desktop ignora a meta no modo auto e só pergunta por causa dela.

Nenhum `allow` do projeto pode cobrir essas ferramentas, nem pelo servidor inteiro (`mcp__controle-financeiro`) nem com curinga. O `FerramentasQueGravamTests` (`tests/ControleFinanceiro.SaidaModelo.Tests/Arquitetura/`) confere as três coisas a partir do `.mcp.json` e do `.claude/settings.json`.

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
| `revisor-dominio` | diff em `src/**`: camadas, agregados, `Result`, Vogen, primitivo com regra em value object, importação idempotente, migrations |
| `revisor-privacidade` | antes de todo PR: dado real ou secret no repositório, dado demais indo para o modelo |
| `revisor-convencoes` | diff em `src/**` ou `tests/**`: convenções do `CLAUDE.md` que o build não confere e as do guia de testes |

Os revisores leem o `--stat` antes do diff. Até ~800 linhas, leem tudo; acima disso, vão arquivo por arquivo em ordem de risco e **dizem o que não leram**. Revisão que para no meio sem avisar passa a impressão de que tudo foi revisado.

Cada revisor carrega uma lista de **dívidas conhecidas** que não deve reportar. Revisor que repete os mesmos achados a cada PR passa a ser ignorado.

As skills `planejar-issue`, `implementar-issue`, `revisar-mudanca` e `abrir-pr` encadeiam agentes e gates ao longo do ciclo de uma issue. A `planejar-issue` e a `implementar-issue` rodam em sessões diferentes, e a passagem entre elas é só o que está publicado no GitHub (ver [trabalhando-com-ia.md](trabalhando-com-ia.md#do-plano-à-implementação)).

## Camada 3: gates

Ative uma vez por clone:

```bash
git config core.hooksPath .githooks
```

| Gate | O que confere |
|---|---|
| `pre-commit` | `scan-staged.sh`: arquivo de `dados/`, extrato fora de Fixtures, chave da Anthropic, token do GitHub, chave privada, senha em appsettings ou `.mcp.json` |
| `commit-msg` | Conventional Commits em PT-BR, descrição com maiúscula e sem ponto final |
| `pre-push` | nada vai para a `main`; branch `<tipo>/<issue>-<slug>` ou `dependabot/...` (para ajustar um PR do Dependabot, como acompanhar no código a versão que ele subiu); testes do guardrail e de `testes-afetados.mjs`; build; testes de unidade; testes de integração dos endpoints afetados e do servidor MCP, se afetado (precisam do Docker) |
| Teste de arquitetura (`SaidaModeloArquiteturaTests`) | quem fala com o modelo só devolve `ISaidaModelo` (a exceção é a `VerificacaoServidor`, só com o estado do banco), não usa agregados, responses da WebApi, SQL direto, prompts, resources nem os outros canais do SDK, e projeto novo em `src/` precisa ser classificado; roda na suíte de unidade, no `pre-push` e no CI |
| CI (`ci.yml`) | build; modelo do EF sem migration pendente; testes de unidade; cobertura mínima do domínio; suíte de integração completa; suíte do servidor MCP; testes do guardrail e de `testes-afetados.mjs`; varredura dos arquivos do PR |

O `pre-push` não imprime nada quando passa. A saída de cada etapa vai para um log temporário (`%TEMP%\pre-push.XXXXXX`), apagado no sucesso. Na falha, o hook diz qual etapa falhou e mostra no stderr cada teste que falhou com a mensagem do erro (ou as últimas 30 linhas do log, num erro de build, por exemplo), além do caminho do log completo, que fica guardado até ser apagado à mão. Assim uma falha intermitente deixa rastro mesmo que a próxima rodada passe.

### Modelo do EF sem migration

Mudar um value object, uma configuração do EF ou uma convenção pode alterar o modelo sem que ninguém gere a migration. Os testes de integração só pegam isso de forma indireta: o EF 10 falha no `MigrateAsync` com uma mensagem que não aponta a causa. Por isso a conferência é um gate próprio:

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/ControleFinanceiro.Infrastructure.Persistence
```

O comando sai com código 1 quando há mudança pendente. Ele usa a `ControleFinanceiroDesignTimeDbContextFactory` e não abre conexão, então não precisa de Docker.

- **`revisar-mudanca`:** roda quando o diff toca `src/ControleFinanceiro.Domain/**` ou `src/ControleFinanceiro.Infrastructure.Persistence/**`.
- **CI:** roda em todo PR, logo depois do build (leva segundos e pega também a mudança que chega por outro caminho, como a atualização de um pacote).
- **Versão:** o `dotnet-ef` é ferramenta local, fixada em `dotnet-tools.json` na versão do `Microsoft.EntityFrameworkCore` do `Directory.Packages.props`. O grupo `microsoft-e-npgsql` do Dependabot inclui os dois, mas o PR pode vir só com o EF, e o gate não reprova a diferença (só avisa). Ao subir o EF, à mão ou pelo Dependabot, confira e suba também o `dotnet-ef` (`dotnet tool update dotnet-ef --version <versão>`).

### Cobertura mínima do domínio

O CI falha quando a cobertura do `ControleFinanceiro.Domain`, medida só por `tests/ControleFinanceiro.Domain.Tests`, fica abaixo de 95% de linhas ou de 95% de branches. As outras suítes não contam, porque os testes de integração passam pelo código sem necessariamente verificar as regras. Não há mínimo global: as migrations são a maior parte das linhas medidas.

Quem confere é o `coverlet.msbuild`, configurado em `ControleFinanceiro.Domain.Tests.csproj` e ligado pelo CI com `-p:CollectCoverage=true`. A saída mostra os valores medidos e o mínimo descumprido. Se o Domain ficar fora da medição, a cobertura sai 0% e o gate reprova. Ele roda só no CI, fora do `pre-push`, onde `Domain.Tests` segue sem cobertura. O `coverlet.collector` continua no projeto para o relatório local do `CoverageReport.ps1`.

```bash
dotnet test tests/ControleFinanceiro.Domain.Tests -p:CollectCoverage=true -p:CoverletOutput=<diretório temporário>/
```

**Como subir o mínimo:** edite `Threshold` no `ControleFinanceiro.Domain.Tests.csproj`, deixando 1 a 2 pontos de folga em relação ao valor que o log do CI mostra, porque o AutoFixture gera dados aleatórios e os branches cobertos podem variar entre execuções. O mínimo sobe aos poucos e não baixa: se um PR reprovar por um branch coberto só por sorte, o conserto é um teste determinístico para esse branch.

### Testes de integração afetados

A suíte de integração sobe um Postgres no Docker, então o `pre-push` não a roda inteira a cada push. O `scripts/harness/testes-afetados.mjs` recebe os arquivos alterados e diz o que rodar:

| Arquivo alterado | Testes de integração |
|---|---|
| `src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/` ou `tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/` | só os de `ControleFinanceiro.WebApi.Tests.<Area>.<Acao>` |
| Domain e Application (regras de value object e validators compartilhados mudam a resposta dos endpoints), Importacao (o leitor de cada layout decide o que a importação grava), Persistence (migrations incluídas), o resto da WebApi, a imagem do Postgres, a infraestrutura dos testes, `Directory.Packages.props` | todos |
| qualquer outro (testes de unidade, servidor MCP, docs, harness) | nenhum |

A suíte do servidor MCP não tem espelho e roda inteira ou não roda. Com `--mcp`, o script imprime `todos` quando o diff toca o próprio servidor ou os testes dele, Domain, Application, Persistence, a imagem do Postgres, `tests/ControleFinanceiro.Tests.Shared`, `tests/Directory.Build.props` ou `Directory.Packages.props`, e nada nos demais casos.

O `pre-push` compara com o que o remoto já tem ou, numa branch nova, com a `origin/main`. Sem base para comparar, roda todos, nas duas suítes. Se for preciso rodar uma delas e o Docker estiver parado, o push falha avisando. O CI roda as duas suítes inteiras em todo PR.

```bash
git diff --name-only origin/main...HEAD | node scripts/harness/testes-afetados.mjs
node scripts/harness/testes-afetados.test.mjs
```

## Manutenção

- **Regra nova do projeto?** Atualize o agente que a revisa. Se ela é verificável mecanicamente, prefira um gate a um parágrafo.
- **Achado repetido nas revisões?** Vira dívida conhecida no agente (para ele parar de repetir) e uma issue `tipo:tech` (para alguém corrigir).
- **O guardrail bloqueou algo legítimo?** Primeiro acrescente o caso em `guardrail.test.mjs` e veja o teste falhar; depois ajuste o padrão. Nunca contorne com `--no-verify`.
- **O dono corrigiu a IA, ou sugeriu algo com que ela concordou?** Ver [refino contínuo](#refino-contínuo).
- **A IA errou a mesma coisa duas vezes?** Vira antipadrão em [trabalhando-com-ia.md](trabalhando-com-ia.md#antipadrões) e, se der, uma regra no guardrail.

## Refino contínuo

Cada correção do dono é uma chance de o harness ensinar o que faltou, para a próxima sessão acertar sem a correção. A regra está no `CLAUDE.md` e vale para toda sessão e todo agente que conversa com o dono:

1. **Quando**: o dono corrige a IA, ou sugere algo e a IA concorda (se ela discorda, conversa, como em qualquer regra).
2. **A pergunta**: *algo nas instruções que eu segui poderia ser diferente para eu ter acertado logo de cara?* Procure a regra que faltou, a que existia mas não dava o critério, ou a que estava num lugar que a tarefa não leva a ler.
3. **Se sim**: avise no chat, diga a causa e proponha o texto e o lugar. Prefira, nesta ordem, um gate (verificável mecanicamente), o guia que a tarefa já manda ler (`padroes-de-dominio.md`, `testes-com-autofixture.md`), o passo da skill em que a decisão é tomada e o item do revisor que cobra. Um exemplo real do repositório vale mais que uma regra abstrata.
4. **Com o ok do dono**: faça a mudança em branch (`docs/<n>-<slug>`) e PR próprios, sem misturar com a issue em andamento.
5. **Se não**: diga em uma linha que as instruções já davam o critério, para o dono saber que a análise foi feita.

Exemplo: no desfazer do pagamento da fatura (#149), a entidade devolvia `bool` e o serviço escrevia o motivo da falha. O dono apontou; a causa foi copiar o análogo pela vizinhança do fluxo. Daí vieram o item "filtro ou falha" da seção 5 do guia de domínio, o passo na `implementar-issue` e o achado no `revisor-dominio`.
