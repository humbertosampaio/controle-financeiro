# Trabalhando com IA neste repositório

O [CLAUDE.md](../CLAUDE.md) é carregado sempre e traz só as regras. Este documento é o manual: lido quando uma tarefa começa, ou quando surge dúvida sobre como conduzir uma.

## Princípios

1. **A IA executa; o dono do repositório decide.** Em tarefa não trivial, o plano é aprovado antes de virar código. Ajustar um plano custa uma mensagem; desfazer código construído sobre uma premissa errada custa muitas.
2. **Partir do que já existe.** Antes de criar uma classe, ler a mais parecida que já existe e seguir o padrão dela. Convenção inventada é retrabalho.
3. **Pronto quer dizer verificado.** Build, testes e diff relido. Quem entrega diz também o que *não* conseguiu verificar.
4. **Todo erro repetido vira registro.** Quando a IA comete o mesmo erro duas vezes, ele entra em [Antipadrões](#antipadrões) e, se der para checar automaticamente, vira gate.
5. **Na dúvida, perguntar.** Escopo ambíguo se resolve perguntando. Ação destrutiva, dependência nova e mudança em dado real só com confirmação explícita.

## Ciclo de uma issue

| Passo | O quê | Ferramenta |
|---|---|---|
| 1 | Descrever o problema numa issue, com critério de pronto | template da issue |
| 2 | Plano revisado (se não trivial) e publicado na issue, com uma sub-issue por etapa grande | `planejar-issue` |
| 3 | Numa sessão nova: partir da `main` atualizada, criar `<tipo>/<n>-<slug>` e implementar com commits semânticos | `implementar-issue` |
| 4 | Gates mecânicos + revisores conforme o diff | `revisar-mudanca` |
| 5 | Push, PR com `Closes #n` e, com o CI verde, merge com merge commit, sem esperar revisão do PR | `abrir-pr` |

**Quando a tarefa é não trivial:** toca mais de 2 arquivos, ou cria classe, endpoint ou migration, ou integra algo externo. Abaixo disso, não há plano: basta criar a branch e seguir as convenções do CLAUDE.md, na mesma sessão.

### Do plano à implementação

A implementação de um plano nunca roda na sessão que planejou. A conversa de planejamento acumula opções descartadas, versões do plano e decisões que mudaram no caminho; uma sessão que herda tudo isso pode seguir uma versão velha. A sessão de implementação começa com o contexto limpo e lê só o que está publicado:

- **Um plano por issue**, num único comentário que começa com `<!-- plano -->`. Revisar o plano edita esse comentário; não se cria um segundo. As decisões tomadas na conversa entram nele, em "Decisões fechadas".
- **A sub-issue é o briefing**: objetivo, arquivos, decisões fechadas, passos, critério de pronto, o que perguntar em vez de improvisar e de que outras sub-issues depende.
- **A sessão nova é proposta, não iniciada**: a `planejar-issue` termina com um cartão para a primeira sub-issue, com o prompt `/implementar-issue <n>` e nada mais. Por padrão ela trabalha no checkout original, para as alterações aparecerem no Visual Studio; worktree só quando pedido.
- **Dúvida se resolve na sessão de implementação**: quando o plano não fecha com o código, ela pergunta no chat e segue a decisão do dono. O comentário do plano não muda depois que a implementação começa.
- **Desvios ficam registrados**: no fim, um comentário `<!-- desvios -->` na issue lista o que ficou diferente do plano, por quê e quem decidiu, e o PR aponta para ele.
- **Uma sub-issue de cada vez**: ao abrir o PR, a sessão de implementação propõe a próxima.

**Dividir antes de começar** quando o enunciado tem "e também", mistura mudança de comportamento com refatoração, toca mais de uma área (domínio + importação + MCP) ou não cabe num critério de pronto de até dois itens. PR grande vira revisão rasa.

## Como pedir uma tarefa

```
Contexto: área e arquivo-âncora
Objetivo: o que deve existir no fim, numa frase
Restrições: regra a respeitar e o que não deve mudar
Pronto quando: como verificar
Referência: arquivo parecido que já funciona
```

Bom:

> Contexto: importação, `src/ControleFinanceiro.Domain/`.
> Objetivo: criar o agregado `Transacao`, com data, valor (`Reais`), descrição, conta de origem e uma chave de deduplicação.
> Restrições: sem EF no domínio; id Vogen gerado no domínio.
> Pronto quando: testes de domínio cobrem criação válida e cada invariante; build sem aviso.
> Referência: `Despesa` em `Despesas/Despesa.cs` e os testes em `Domain.Tests/Despesas/`.

Ruim: "cria uma entidade de transação". Sem âncora, a IA escolhe namespace, tipo de id e regras por conta própria.

## O que exige confirmação antes

| Ação | Por quê |
|---|---|
| `git push --force`, `reset --hard`, `clean -f`, apagar branch | Reescreve ou apaga trabalho |
| Push na `main` | Tudo entra por PR |
| `dotnet add package` / pacote novo | Manutenção e cadeia de suprimentos |
| Migration que apaga ou renomeia coluna ou tabela | O banco local tem dados reais |
| Ler ou mover arquivos de `dados/` para qualquer lugar versionado | Privacidade |
| Editar `.claude/settings.json`, `.githooks/`, `scripts/harness/` ou o CI | Muda o próprio harness |

O guardrail (`scripts/harness/guardrail.mjs`) transforma a maior parte dessa tabela em mecanismo. O que ele não pega continua valendo como regra.

## Privacidade dos dados financeiros

- Os arquivos reais ficam em `dados/`. A IA pode ler esses arquivos para entender um layout, mas, ao reportar, descreve **estrutura** (colunas, formatos, contagens), nunca valores, nomes ou números de conta.
- Fixture de teste é fictícia: mesmo cabeçalho, mesmos formatos de data e número, mesmo encoding, conteúdo inventado.
- O que vai para o modelo em produção (MCP e agente) passa por uma projeção explícita, com os campos mínimos. Campo novo nessa projeção é decisão de PR, revisada pelo `revisor-privacidade`.

## Checklist depois da IA

Mesmo com tudo verde, ler o diff.

**Sempre**
- [ ] Cada trecho do diff tem motivo
- [ ] Nenhum `TODO` sem issue, nenhum `Console.WriteLine` esquecido
- [ ] Nada de `dados/`, secret ou dado pessoal no diff
- [ ] Commits semânticos em Conventional Commits PT-BR, com `Refs #n`

**Backend**
- [ ] `dotnet build ControleFinanceiro.slnx` sem avisos
- [ ] `dotnet test ControleFinanceiro.slnx` verde
- [ ] Domain sem `using` de EF Core, ASP.NET ou Npgsql
- [ ] Regra nova com teste do caminho válido e de cada invariante
- [ ] Migration nova revisada à mão (`Up` e `Down`), sem operação destrutiva não combinada
- [ ] Adaptador externo trata timeout e erro com `Result`, sem exceção para falha previsível

**Frontend** (quando existir)
- [ ] Build e lint limpos
- [ ] Telas afetadas verificadas no navegador, em desktop e celular
- [ ] Nenhum dado real em mock ou screenshot

## Camadas de contexto

| Camada | Onde | O que guarda | Quando muda |
|---|---|---|---|
| Regras | `CLAUDE.md` | Regras invioláveis, fluxo, convenções. Curto, porque é carregado sempre | Quando uma regra muda |
| Manual | `docs/trabalhando-com-ia.md`, `docs/harness.md` | Como conduzir o trabalho, antipadrões | Quando uma prática evolui |
| Decisões | `docs/decisoes/` (ADRs, quando surgirem) | Por que algo é como é | A cada decisão de arquitetura |

A memória pessoal do Claude Code guarda preferências de quem usa, não regras do projeto. Regra do projeto mora no repositório.

## Antipadrões

Catálogo vivo: cada item tem sintoma, por que é errado e como corrigir. Entra aqui o que realmente aconteceu neste repositório, não regra genérica.

<!-- Formato:
**❌ Título curto**
- Sintoma: o que a IA fez
- Por que é errado: qual regra quebra
- Como corrigir: o que fazer no lugar
-->

_Nenhum registrado ainda._
