# Controle Financeiro

Organizador financeiro pessoal: importa extratos e faturas de vários bancos, cadastra renda e despesas fixas, liga cada transação à despesa que ela pagou e mostra o balanço do mês num lugar só. Por enquanto é de um usuário só (o dono do repositório).

**O centro do MVP é o servidor MCP e o agente de conciliação**, não o CRUD. A integração com Open Finance (Pluggy), a B3, metas e o frontend vêm depois. Na dúvida sobre prioridade, a pergunta é: isso aproxima o agente de conciliar transações reais com uma taxa de acerto medida?

## Solução

.NET 10, `ControleFinanceiro.slnx` na raiz, pacotes centralizados em `Directory.Packages.props` com `TreatWarningsAsErrors`.

| Projeto | Papel | Depende de |
|---|---|---|
| `src/ControleFinanceiro.Domain` | Agregados, value objects, serviços de domínio | nada de infraestrutura |
| `src/ControleFinanceiro.Application` | Portas (`IUnitOfWork`), validação de entrada, tipos de saída para o modelo (`IA/Saida/`, e as leituras que vão ao modelo em `IA/Leituras/`) | Domain |
| `src/ControleFinanceiro.Infrastructure.Importacao` | Leitores de extrato e fatura, um adaptador por layout | Application |
| `src/ControleFinanceiro.Infrastructure.Persistence` | EF Core + Postgres, repositórios, migrations | Domain, Application |
| `src/ControleFinanceiro.WebApi` | Endpoints FastEndpoints | Application, Persistence |
| `src/ControleFinanceiro.Mcp` | Servidor MCP local por stdio, iniciado pelo cliente MCP | Application, Persistence, Importacao (só o registro dos leitores e da origem dos arquivos) |
| `src/ControleFinanceiro.AppHost` | Aspire: sobe o Postgres e a API localmente | WebApi |
| `src/ControleFinanceiro.ServiceDefaults` | Telemetria e health checks do Aspire | — |
| `src/ControleFinanceiro.Analyzers` | Analisadores Roslyn das convenções, ligados em todo projeto pelo `Directory.Build.props` | — |
| `tests/ControleFinanceiro.Domain.Tests` | Testes de domínio (xUnit + AutoFixture + FluentAssertions) | Domain |
| `tests/ControleFinanceiro.Infrastructure.Importacao.Tests` | Testes dos leitores de extrato com arquivos fictícios em `Fixtures/` | Importacao, Tests.Shared |
| `tests/ControleFinanceiro.WebApi.Tests` | Testes de integração dos endpoints: API em memória + Postgres do Testcontainers (precisa do Docker) | WebApi, Tests.Shared |
| `tests/ControleFinanceiro.Mcp.Tests` | Testes do servidor MCP: processo real + Postgres do Testcontainers (precisa do Docker) | Mcp, Tests.Shared |
| `tests/ControleFinanceiro.SaidaModelo.Tests` | Payload dos tipos de saída para o modelo, regras de arquitetura da fronteira (ArchUnitNET) e dependências entre camadas; sem Docker | Mcp, Importacao, Tests.Shared |
| `tests/ControleFinanceiro.Analyzers.Tests` | Testes dos analisadores, com trechos de código compilados em memória; sem Docker | Analyzers, Tests.Shared |
| `tests/ControleFinanceiro.Tests.Shared` | Customizações do AutoFixture compartilhadas | Domain |

## Regras invioláveis

Discordar de uma regra é motivo para conversar, não para contorná-la no código.

1. **Dados financeiros reais nunca entram no git.** Extratos e faturas ficam em `dados/` (ignorada), assim como `*.ofx`, `*.csv`, `*.xlsx` e bancos locais. Teste usa arquivo fictício em `tests/**/Fixtures/`, com o mesmo layout e nenhum dado real. Não copie trechos de `dados/` para código, teste, issue, PR ou commit.
2. **Secrets ficam em User Secrets**, nunca em `appsettings*.json`, código ou docs. Isso vale para a senha do Postgres, a chave da Anthropic e as credenciais da Pluggy.
3. **O modelo recebe o mínimo.** Das transações vão ao Claude só descrição, valor e data (e o indispensável, justificado no PR), sempre por um tipo de `Application/IA/Saida` (`ISaidaModelo`), que mascara os documentos; número de conta e agência nunca saem. O detalhe, a exceção e o risco aceito estão em `src/ControleFinanceiro.Application/IA/CLAUDE.md`.
4. **O domínio não conhece infraestrutura.** `ControleFinanceiro.Domain` não referencia EF Core, ASP.NET nem Npgsql (o `DependenciasCamadasTests` confere). Mapeamento fica em `IEntityTypeConfiguration` na Persistence.
5. **Falha previsível é `Result`, não exceção** (CSharpFunctionalExtensions). Exceção fica para erro de sistema ([padrões de domínio, §5](docs/padroes-de-dominio.md#5-falhas)).
6. **Ids fortemente tipados** com Vogen, gerados no domínio (`FromNewVersion7Guid`), sem `Guid` solto na API pública do domínio ([§7](docs/padroes-de-dominio.md#7-value-objects-e-ids)).
7. **Integração externa entra por porta + adaptador**: a porta mora no Domain ou na Application, o adaptador na infraestrutura. Cada formato de extrato é um adaptador da mesma porta de importação.
8. **Migration que apaga ou renomeia coluna ou tabela pede confirmação** antes de ser gerada (a `revisar-mudanca` procura no diff).

## Fluxo de trabalho

1. **Toda mudança de comportamento nasce de uma issue** (`.github/ISSUE_TEMPLATE/`), menos typo, comentário ou doc isolado.
2. **Branch a partir da `main` atualizada**, `<tipo>/<numero-da-issue>-<slug>`, com tipo `feat`, `fix`, `chore`, `docs`, `refactor` ou `test` (`feat/7-importar-ofx-c6`).
3. **Plano antes do código** em tarefa não trivial (mais de 2 arquivos, classe, endpoint, migration ou integração externa), pela `planejar-issue`, com a aprovação do dono; a implementação roda numa sessão nova, pela `implementar-issue`.
4. **Commits em Conventional Commits, em PT-BR**, um por assunto, com `Refs #N` no corpo: `feat(importacao): Lê o extrato OFX da conta do C6`.
5. **Antes do PR, a `revisar-mudanca`**: build e testes verdes e nenhum achado de revisor sem resolver. Achado que você não vai atender, de qualquer severidade: explique ao dono e espere a decisão dele.
6. **PR pela `abrir-pr`**, com `Closes #N`: com o CI verde, a sessão faz o merge sozinha, com merge commit, e a correção do que nasceu na branch é dobrada no commit original (`--fixup`).
7. **PRs do Dependabot** não nascem de issue nem seguem o nome de branch; o changelog é revisado antes do merge ([docs/harness.md](docs/harness.md#dependabot)).

O manual do ciclo está em [docs/trabalhando-com-ia.md](docs/trabalhando-com-ia.md).

## Onde estão as regras

O Claude Code carrega o `CLAUDE.md` da pasta do arquivo que lê. Os agentes e a `implementar-issue` não dependem disso: leem os que valem pelo `scripts/harness/claude-md-aplicaveis.mjs`.

| Assunto | Onde |
|---|---|
| Convenções de C#: nomes, forma do código, conversões, DI, cadeia de `Result` | `src/CLAUDE.md` |
| Testes: onde mora cada um, Docker, dados de teste | `tests/CLAUDE.md` e [docs/testes-com-autofixture.md](docs/testes-com-autofixture.md) |
| Modelagem do domínio | `src/ControleFinanceiro.Domain/CLAUDE.md` e [docs/padroes-de-dominio.md](docs/padroes-de-dominio.md) |
| Regras com gate no build (analisadores CF) | `src/ControleFinanceiro.Analyzers/README.md` |
| Saída para o modelo | `src/ControleFinanceiro.Application/IA/CLAUDE.md` |
| Servidor MCP | `src/ControleFinanceiro.Mcp/CLAUDE.md` e [docs/servidor-mcp.md](docs/servidor-mcp.md) |
| Endpoints | `src/ControleFinanceiro.WebApi/CLAUDE.md` |
| Leitores de extrato e fatura | `src/ControleFinanceiro.Infrastructure.Importacao/CLAUDE.md` |
| EF Core e migrations | `src/ControleFinanceiro.Infrastructure.Persistence/CLAUDE.md` |

Valem no repositório inteiro:

- **PT-BR em tudo**: domínio, identificadores (`Usuario`, `Despesa`, `MesReferencia`), mensagens, commits, issues, PRs e comentários. Termos técnicos consagrados ficam em inglês (`Result`, `Repository`, `Endpoint`, `AggregateRoot`, `DomainEvent`, `EventHandler`, `Dispatcher`).
- **Siga o exemplo existente**: antes de criar algo, leia o análogo mais próximo e cite-o no plano. Não invente API nem pacote. Dependência nova pede justificativa e confirmação.

## Comandos

```bash
dotnet build ControleFinanceiro.slnx
dotnet test ControleFinanceiro.slnx                   # unidade + integração (precisa do Docker)
dotnet test tests/ControleFinanceiro.WebApi.Tests --filter "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar."   # um endpoint
dotnet test tests/ControleFinanceiro.Mcp.Tests        # servidor MCP (precisa do Docker)
dotnet run --project src/ControleFinanceiro.AppHost   # Postgres + API; dashboard do Aspire no console
pwsh scripts/CoverageReport.ps1                       # relatório de cobertura em coveragereport/
node scripts/harness/guardrail.test.mjs               # testes do guardrail
dotnet tool restore                                   # dotnet-ef local, na versão do EF (dotnet-tools.json)
dotnet ef migrations has-pending-model-changes --project src/ControleFinanceiro.Infrastructure.Persistence   # modelo mudou sem migration?
```

O servidor MCP não abre porta nem sobe pelo AppHost: quem o inicia é o cliente MCP (`.mcp.json`), a partir da cópia que `pwsh scripts/PublicarMcp.ps1` publica; ver [docs/servidor-mcp.md](docs/servidor-mcp.md).

Os testes de integração, o `pre-push` e a cobertura precisam do **Docker** rodando: sem ele, a suíte de integração falha (não é pulada).

Antes de rodar o AppHost pela primeira vez:
- instale a Aspire CLI na mesma versão do `Aspire.AppHost.Sdk`: `dotnet tool install -g Aspire.Cli --version <versão>` (ao atualizar o Aspire, atualize também a CLI com `dotnet tool update -g Aspire.Cli`);
- defina a senha do Postgres: `dotnet user-secrets set "Parameters:PostgresPassword" "<senha>" --project src/ControleFinanceiro.AppHost`.

## Harness

O guardrail (`scripts/harness/guardrail.mjs`) bloqueia o erro caro, os agentes de `.claude/agents/` revisam e só reportam, e os gates (`.githooks/` e o CI) conferem cada commit e PR; ative os hooks uma vez por clone com `git config core.hooksPath .githooks`. As skills `planejar-issue`, `implementar-issue`, `revisar-mudanca` e `abrir-pr` encadeiam tudo. Detalhes em [docs/harness.md](docs/harness.md).

**Refino contínuo.** Quando o dono te corrigir, ou sugerir algo com que você concorde, pergunte-se o que nas instruções poderia ser diferente para você ter acertado de cara, e proponha no chat o texto e o lugar. Com o ok dele, a mudança vira um commit `docs(harness)` próprio na branch da issue em andamento ([docs/harness.md](docs/harness.md#refino-contínuo)).

## GitHub

Repositório público `humbertosampaio/controle-financeiro`, sob a licença MIT (`LICENSE`). Trabalho organizado em Issues (labels `tipo:*`, `fase:*`, `area:*`), sub-issues sob um épico por fase e um GitHub Project. Use o `gh` CLI. Toda issue é sub-issue de um épico de fase, e só os épicos ficam sem pai: a label `fase:*` sozinha não vincula (`gh api repos/{owner}/{repo}/issues/<epico>/sub_issues -X POST -F sub_issue_id=<id numérico>`).
