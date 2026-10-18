# Controle Financeiro

Organizador financeiro pessoal: importa extratos e faturas de vários bancos, cadastra renda e despesas fixas, liga cada transação à despesa que ela pagou e mostra o balanço do mês num lugar só. Por enquanto é de um usuário só (o dono do repositório).

**O centro do MVP é o servidor MCP e o agente de conciliação**, não o CRUD. A integração com Open Finance (Pluggy), a B3, metas e o frontend vêm depois. Na dúvida sobre prioridade, a pergunta é: isso aproxima o agente de conciliar transações reais com uma taxa de acerto medida?

## Solução

.NET 10, `ControleFinanceiro.slnx` na raiz, pacotes centralizados em `Directory.Packages.props` com `TreatWarningsAsErrors`.

| Projeto | Papel | Depende de |
|---|---|---|
| `src/ControleFinanceiro.Domain` | Agregados, value objects, serviços de domínio | nada de infraestrutura |
| `src/ControleFinanceiro.Application` | Portas (`IUnitOfWork`), validação de entrada | Domain |
| `src/ControleFinanceiro.Infrastructure.Persistence` | EF Core + Postgres, repositórios, migrations | Domain, Application |
| `src/ControleFinanceiro.WebApi` | Endpoints FastEndpoints | Application, Persistence |
| `src/ControleFinanceiro.AppHost` | Aspire: sobe o Postgres e a API localmente | WebApi |
| `src/ControleFinanceiro.ServiceDefaults` | Telemetria e health checks do Aspire | — |
| `tests/ControleFinanceiro.Domain.Tests` | Testes de domínio (xUnit + AutoFixture + FluentAssertions) | Domain |
| `tests/ControleFinanceiro.Tests.Shared` | Customizações do AutoFixture compartilhadas | Domain |

## Regras invioláveis

Discordar de uma regra é motivo para conversar, não para contorná-la no código.

1. **Dados financeiros reais nunca entram no git.** Extratos e faturas ficam em `dados/` (ignorada), assim como `*.ofx`, `*.csv`, `*.xlsx` e bancos locais. Teste usa arquivo fictício em `tests/**/Fixtures/`, com o mesmo layout e nenhum dado real. Não copie trechos de `dados/` para código, teste, issue, PR ou commit.
2. **Secrets ficam em User Secrets**, nunca em `appsettings*.json`, código ou docs. Isso vale para a senha do Postgres, a chave da Anthropic e as credenciais da Pluggy.
3. **O modelo recebe o mínimo.** Quando o agente ou o MCP mandam transações para o Claude, vão só descrição, valor e data (e o que mais for indispensável, justificado no PR). Nada de número de conta, agência, CPF ou nome de titular.
4. **O domínio não conhece infraestrutura.** `ControleFinanceiro.Domain` não referencia EF Core, ASP.NET nem Npgsql. Mapeamento fica em `IEntityTypeConfiguration` na Persistence.
5. **Falha previsível é `Result`, não exceção** (CSharpFunctionalExtensions). Exceção fica para erro de sistema.
6. **Ids fortemente tipados** com Vogen, gerados no domínio (`FromNewVersion7Guid`). Sem `Guid` solto na API pública do domínio.
7. **Integração externa entra por porta + adaptador**: a porta mora no Domain ou na Application, o adaptador na infraestrutura. Cada formato de extrato é um adaptador da mesma porta de importação.
8. **Migration que apaga ou renomeia coluna ou tabela pede confirmação** antes de ser gerada.

## Fluxo de trabalho

1. **Toda mudança de comportamento nasce de uma issue** (templates em `.github/ISSUE_TEMPLATE/`). Exceção: typo, comentário ou doc isolado.
2. **Branch a partir da `main` atualizada**: `<tipo>/<numero-da-issue>-<slug>`, com tipo `feat`, `fix`, `chore`, `docs`, `refactor` ou `test`. Ex.: `feat/7-importar-ofx-c6`.
3. **Plano antes do código** quando a tarefa toca mais de 2 arquivos, cria classe, endpoint ou migration, ou integra algo externo. Use o agente `planejador` ou a skill `planejar-issue` e espere aprovação.
4. **Commits em Conventional Commits, em PT-BR**, divididos por assunto: cada commit agrupa mudanças que fazem sentido juntas. Formato `tipo(escopo): Descrição no presente`, ex. `feat(importacao): Lê o extrato OFX da conta do C6`. Referencie a issue no corpo (`Refs #7`).
5. **Verificação antes do PR**: `dotnet build` e `dotnet test` verdes e o diff relido. Rode a skill `revisar-mudanca`.
6. **PR com `Closes #N`** no corpo, aberto pela skill `abrir-pr`. Nada vai direto para a `main`.

Detalhes, template de pedido e checklist: [docs/trabalhando-com-ia.md](docs/trabalhando-com-ia.md).

## Convenções

- **PT-BR em tudo**: domínio, identificadores (`Usuario`, `Despesa`, `MesReferencia`), mensagens, commits, issues, PRs e comentários. Termos técnicos consagrados ficam em inglês (`Result`, `Repository`, `Endpoint`).
- **Nome de teste**: `Metodo_Quando_Cenario_Deve_Resultado`, com `[Theory, DomainAutoData]` para gerar entradas. Um comportamento por teste; sem relógio real (`TimeProvider`).
- **Dinheiro é `Reais`/`ReaisPositivo`**, nunca `decimal` solto no domínio.
- **Siga o exemplo existente**: antes de criar algo, leia o análogo mais próximo e cite-o no plano. Não invente API nem pacote. Dependência nova pede justificativa e confirmação.

## Comandos

```bash
dotnet build ControleFinanceiro.slnx
dotnet test ControleFinanceiro.slnx
dotnet run --project src/ControleFinanceiro.AppHost   # Postgres + API; dashboard do Aspire no console
pwsh scripts/CoverageReport.ps1                       # relatório de cobertura em coveragereport/
node scripts/harness/guardrail.test.mjs               # testes do guardrail
```

Antes de rodar o AppHost pela primeira vez, defina a senha do Postgres: `dotnet user-secrets set "Parameters:PostgresPassword" "<senha>" --project src/ControleFinanceiro.AppHost`.

## Harness

Três camadas, descritas em [docs/harness.md](docs/harness.md):

- **Guardrail** (`.claude/settings.json` → `scripts/harness/guardrail.mjs`): bloqueia commit de dados financeiros, secret em arquivo e push na `main`; pede confirmação em git destrutivo, pacote novo e migration.
- **Agentes** (`.claude/agents/`): `planejador`, `autor-de-testes`, `revisor-dominio` e `revisor-privacidade`. Os revisores só reportam.
- **Skills** (`.claude/skills/`): `planejar-issue` (plano na issue + sub-issues, antes do código), `revisar-mudanca` (gates + revisores) e `abrir-pr` (push + PR vinculado), depois do código.
- **Gates** (`.githooks/` + `.github/workflows/ci.yml`): varredura de secrets e de dados no pre-commit, Conventional Commits no commit-msg, bloqueio de push na `main` no pre-push, build e testes no CI. Ative uma vez por clone: `git config core.hooksPath .githooks`.

## GitHub

Repositório privado `humbertosampaio/controle-financeiro`. Trabalho organizado em Issues (labels `tipo:*`, `fase:*`, `area:*`), sub-issues sob um épico por fase e um GitHub Project. Use o `gh` CLI.
