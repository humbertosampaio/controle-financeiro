# Controle Financeiro

Organizador financeiro pessoal: importa extratos e faturas de vários bancos, cadastra renda e despesas fixas, liga cada transação à despesa que ela pagou e mostra o balanço do mês num lugar só. Por enquanto é de um usuário só (o dono do repositório).

**O centro do MVP é o servidor MCP e o agente de conciliação**, não o CRUD. A integração com Open Finance (Pluggy), a B3, metas e o frontend vêm depois. Na dúvida sobre prioridade, a pergunta é: isso aproxima o agente de conciliar transações reais com uma taxa de acerto medida?

## Solução

.NET 10, `ControleFinanceiro.slnx` na raiz, pacotes centralizados em `Directory.Packages.props` com `TreatWarningsAsErrors`.

| Projeto | Papel | Depende de |
|---|---|---|
| `src/ControleFinanceiro.Domain` | Agregados, value objects, serviços de domínio | nada de infraestrutura |
| `src/ControleFinanceiro.Application` | Portas (`IUnitOfWork`), validação de entrada | Domain |
| `src/ControleFinanceiro.Infrastructure.Importacao` | Leitores de extrato e fatura, um adaptador por layout | Application |
| `src/ControleFinanceiro.Infrastructure.Persistence` | EF Core + Postgres, repositórios, migrations | Domain, Application |
| `src/ControleFinanceiro.WebApi` | Endpoints FastEndpoints | Application, Persistence |
| `src/ControleFinanceiro.AppHost` | Aspire: sobe o Postgres e a API localmente | WebApi |
| `src/ControleFinanceiro.ServiceDefaults` | Telemetria e health checks do Aspire | — |
| `tests/ControleFinanceiro.Domain.Tests` | Testes de domínio (xUnit + AutoFixture + FluentAssertions) | Domain |
| `tests/ControleFinanceiro.Infrastructure.Importacao.Tests` | Testes dos leitores de extrato com arquivos fictícios em `Fixtures/` | Importacao, Tests.Shared |
| `tests/ControleFinanceiro.WebApi.Tests` | Testes de integração dos endpoints: API em memória + Postgres do Testcontainers (precisa do Docker) | WebApi, Tests.Shared |
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
7. **Merge com merge commit, nunca squash nem rebase** (o repositório só oferece essa opção). Assim os commits semânticos chegam à `main` e cada PR continua visível: `git log --first-parent main` mostra uma linha por PR. Por isso, antes de abrir o PR, a correção de algo criado na própria branch é dobrada no commit original (`git commit --fixup <sha>` + `git rebase -i --autosquash origin/main`), em vez de virar um commit `fix` solto.

**Exceção: PRs do Dependabot** (`.github/dependabot.yml`) não nascem de issue nem seguem o nome de branch acima. O CI roda neles como em qualquer PR; revise o changelog dos pacotes antes do merge. Atualizações bloqueadas de propósito (FluentAssertions 8+, que é pago; MessagePack 3+) estão justificadas no próprio arquivo.

Detalhes, template de pedido e checklist: [docs/trabalhando-com-ia.md](docs/trabalhando-com-ia.md).

## Convenções

- **PT-BR em tudo**: domínio, identificadores (`Usuario`, `Despesa`, `MesReferencia`), mensagens, commits, issues, PRs e comentários. Termos técnicos consagrados ficam em inglês (`Result`, `Repository`, `Endpoint`).
- **Nome de teste**: `Metodo_Quando_Cenario_Deve_Resultado` (sem cenário, o `Quando` sai: `Criar_Deve_GerarIdVersao7ParaCadaTransacao`), com `[Theory, DomainAutoData]` para gerar entradas. Um comportamento por teste; sem relógio real (`TimeProvider`). Valor fixo vem de `[Frozen]`; `fixture.Inject` só quando o `[Frozen]` não serve. **Leia [docs/testes-com-autofixture.md](docs/testes-com-autofixture.md) antes de escrever teste.**
- **Endpoint novo ou alterado tem teste de integração** na pasta espelhada: `src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/` → `tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/`, com `[ApiAutoData]` e lendo de volta do banco. O espelho é o que faz o `pre-push` rodar só os testes afetados.
- **Modelagem segue [docs/padroes-de-dominio.md](docs/padroes-de-dominio.md)**: referência entre agregados só pelo id da raiz, um agregado alterado por transação, regra entre agregados em factory de domínio, repositório só com agregados e efeito em outro agregado por evento. **Leia antes de criar ou mudar agregado.** O `revisor-dominio` cobra.
- **Diagrama do domínio no mesmo PR**: mudou agregado, entidade, value object, relação ou invariante no Domain, atualize [docs/modelo-de-dominio.md](docs/modelo-de-dominio.md) (Mermaid). O `revisor-dominio` cobra.
- **Dinheiro é `Reais`/`ReaisPositivo`**, nunca `decimal` solto no domínio. Vale para todo primitivo com regra (formato, faixa, obrigatoriedade): no domínio ele vira value object que valida na criação, como `SufixoCartao` e `DiaVencimentoCartao`. O `revisor-dominio` cobra.
- **Block body em construtor e em método multilinha.** Construtor usa sempre block body, mesmo com uma linha só; o build quebra se não usar (IDE0021, com `EnforceCodeStyleInBuild` no `Directory.Build.props`). Método, operador ou função local cujo corpo passa de uma linha também usa block body, e isso inclui uma instrução única quebrada em várias linhas. O `=>` serve quando a expressão ocupa uma linha só, na mesma linha da assinatura ou logo abaixo dela. Essa regra o build não confere (o IDE0022 do Roslyn só aponta o sentido contrário), então o `revisor-convencoes` cobra. Lambdas ficam de fora.
- **Argumento literal vai nomeado.** Coleção vazia, `null`, booleano ou número passado direto numa chamada leva o nome do parâmetro (`contasDoUsuario: []`, `cartao: null`, `ordinal: 1`), porque o literal sozinho não diz o que representa. Variável cujo nome já diz o que é vai sem nome. Ficam de fora o argumento único cujo papel o nome do método já diz (`AddDays(1)`, `ReaisPositivo.Criar(-0.004m)`), datas e horas (`new DateOnly(2026, 3, 5)`), guard clauses (`ThrowIfLessThan(ordinal, 1)`), a lista de um `params` ou de um `TheoryData`, a declaração de um SmartEnum, atributos, asserções (`Should().Be(850m)`) e lambdas. O `revisor-convencoes` cobra.
- **Diretriz, não regra: prefira uma cadeia plana de métodos na orquestração com `Result`.** É o ponto de partida ao encadear etapas que devolvem `Result` ou `Maybe` do CSharpFunctionalExtensions, mas a legibilidade decide: quando a forma plana ficar mais difícil de ler que a alternativa (um `Bind` aninhado curto, um `if (result.IsFailure) return ...`, um laço comum), use a alternativa. A forma preferida:
  - as etapas se encadeiam com `Bind`, `Map`, `Ensure`, `MapError` e `Match` no mesmo nível, sem um `Bind` dentro do lambda de outro e sem query syntax (`from ... select`);
  - cada etapa é um método privado que recebe só o que a anterior produziu, passado como method group quando a assinatura bate (`.Bind(LerContaEPeriodo)`);
  - quando a etapa seguinte precisa de mais de um valor, a anterior devolve uma tupla nomeada com alias no topo do arquivo (`using ContaEPeriodo = (ControleFinanceiro.Domain.Contas.SufixoConta Sufixo, ...);`, com namespace completo, porque o alias não enxerga os outros `using`s), em vez de record auxiliar ou de captura por closure;
  - o método que monta a tupla pode aninhar um nível: `LerSufixoConta(c).Bind(sufixo => LerPeriodo(c).Map(periodo => (sufixo, periodo)))`. O `Result.Combine` só serve quando todas as falhas devem aparecer juntas;
  - laço sobre I/O (ler as linhas de um arquivo) pode ser imperativo, dentro de um método que devolve `Result`.

  Exemplo: `LeitorExtratoContaC6.LerAsync`. O `revisor-convencoes` sugere a forma plana quando ela deixaria o código mais claro, sem tratar a outra forma como erro.
- **Siga o exemplo existente**: antes de criar algo, leia o análogo mais próximo e cite-o no plano. Não invente API nem pacote. Dependência nova pede justificativa e confirmação.

## Comandos

```bash
dotnet build ControleFinanceiro.slnx
dotnet test ControleFinanceiro.slnx                   # unidade + integração (precisa do Docker)
dotnet test tests/ControleFinanceiro.WebApi.Tests --filter "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar."   # um endpoint
dotnet run --project src/ControleFinanceiro.AppHost   # Postgres + API; dashboard do Aspire no console
pwsh scripts/CoverageReport.ps1                       # relatório de cobertura em coveragereport/
node scripts/harness/guardrail.test.mjs               # testes do guardrail
```

Os testes de integração e o `pre-push` precisam do **Docker** rodando: sem ele, a suíte de integração falha (não é pulada). A cobertura (`CoverageReport.ps1`) também.

Antes de rodar o AppHost pela primeira vez:
- instale a Aspire CLI na mesma versão do `Aspire.AppHost.Sdk`: `dotnet tool install -g Aspire.Cli --version <versão>` (o AppHost usa o orquestrador dela; ao atualizar o Aspire, atualize também a CLI com `dotnet tool update -g Aspire.Cli`);
- defina a senha do Postgres: `dotnet user-secrets set "Parameters:PostgresPassword" "<senha>" --project src/ControleFinanceiro.AppHost`.

## Harness

Três camadas, descritas em [docs/harness.md](docs/harness.md):

- **Guardrail** (`.claude/settings.json` → `scripts/harness/guardrail.mjs`): bloqueia commit de dados financeiros, secret em arquivo e push na `main`; pede confirmação em git destrutivo, pacote novo e migration.
- **Agentes** (`.claude/agents/`): `planejador`, `autor-de-testes`, `revisor-dominio`, `revisor-convencoes` e `revisor-privacidade`. Os revisores só reportam.
- **Skills** (`.claude/skills/`): `planejar-issue` (plano na issue + sub-issues, antes do código), `revisar-mudanca` (gates + revisores) e `abrir-pr` (push + PR vinculado), depois do código.
- **Gates** (`.githooks/` + `.github/workflows/ci.yml`): varredura de secrets e de dados no pre-commit, Conventional Commits no commit-msg, bloqueio de push na `main`, testes de unidade e testes de integração afetados (`scripts/harness/testes-afetados.mjs`) no pre-push, build e as duas suítes completas no CI. Ative uma vez por clone: `git config core.hooksPath .githooks`.

## GitHub

Repositório privado `humbertosampaio/controle-financeiro`. Trabalho organizado em Issues (labels `tipo:*`, `fase:*`, `area:*`), sub-issues sob um épico por fase e um GitHub Project. Use o `gh` CLI.
