---
name: implementar-issue
description: Implementa uma issue do controle-financeiro a partir do plano publicado nela, numa sessão nova e com o contexto limpo. Lê só a issue e o comentário do plano, cria a branch, segue os passos com commits semânticos, tira as dúvidas com o dono no chat e registra os desvios do plano na issue. Use quando o pedido for "/implementar-issue N" ou "implementar a issue N".
---

# implementar-issue

Leva uma issue de "plano publicado" a "branch revisada com PR aberto". Roda numa sessão nova, aberta depois da `planejar-issue`: o que esta sessão sabe do trabalho vem do GitHub, não da conversa em que o plano nasceu.

## 1. Ler o briefing, e só ele

Valide que `<n>` é numérico antes de usá-lo em comando.

```bash
gh issue view <n> --json number,title,body,labels
```

O plano é o comentário que começa com `<!-- plano -->`, na issue pai indicada na linha `Plano: #<pai>` do corpo, ou na própria issue quando ela não for sub-issue:

```bash
gh api repos/{owner}/{repo}/issues/<pai-ou-n>/comments --paginate --jq '.[] | select(.body | startswith("<!-- plano -->")) | .body'
```

Não leia os demais comentários: a discussão do planejamento já está consolidada no plano, e lê-la de novo traz de volta as opções descartadas. Só abra um comentário que o plano ou a sub-issue citem.

Sem comentário de plano e com tarefa não trivial (mais de 2 arquivos, classe, endpoint ou migration nova, integração externa), pare e sugira a `planejar-issue`.

Leia também os arquivos-âncora que o plano cita antes de escrever qualquer coisa, e o `docs/testes-com-autofixture.md` antes de escrever teste.

## 2. Preparar a branch

A sessão trabalha, por padrão, no checkout original, que o dono acompanha no Visual Studio. Antes de mexer nele:

```bash
git status --short
git branch --show-current
```

Working tree sujo ou branch de outra issue: **pare e pergunte**. Pode ser trabalho do dono em andamento.

Se a sub-issue depende de outra (seção "Depende de") que ainda não está na `main`, pergunte se a branch parte da branch dela ou se a implementação espera o merge.

Branch `<tipo>/<n>-<slug>` a partir da `main` atualizada, sem rastrear a `origin/main` (o `abrir-pr` define o upstream no push). O tipo vem da label: `tipo:us` → `feat`, `tipo:bug` → `fix`, `tipo:tech` → `chore`, `refactor`, `test` ou `docs`, conforme o conteúdo; na dúvida, pergunte.

```bash
git fetch origin main --quiet
git switch --no-track -c <tipo>/<n>-<slug> origin/main
```

Se a branch já existir (a sessão está retomando o trabalho), troque para ela e continue do primeiro passo sem commit.

## 3. Implementar

Siga os passos na ordem do plano. Cada passo vira um commit semântico em Conventional Commits PT-BR, com `Refs #<n>` no corpo. Onde o plano marca um ponto de parada, mostre o resultado e espere o ok antes de seguir.

Código novo segue o análogo citado no plano. Regra nova tem teste no mesmo commit, ou no seguinte quando o plano separa; use o `autor-de-testes` quando faltar cobertura.

Ao criar uma classe de testes com muitos casos, escreva dois ou três, releia-os com o critério de [cada parâmetro precisa se justificar](../../../docs/testes-com-autofixture.md#cada-parâmetro-precisa-se-justificar) e só então replique o formato nos demais.

Antes de criar um tipo no Domain, confira a forma dele com as perguntas de [a forma do tipo vem do conceito](../../../docs/padroes-de-dominio.md#7-value-objects-e-ids) (entidade, SmartEnum, value object ou `record`), mesmo que o plano já a tenha definido: a forma que o plano escolheu é desenho a conferir, e não decisão fechada, a não ser que esteja em "Decisões fechadas". Se o critério der outra forma, pergunte ao dono como na seção 4 e registre o desvio.

Num passo que cria serviço de domínio, depois de os testes passarem e antes de mostrar o passo, releia cada método privado do serviço com o critério de [o serviço de domínio só orquestra](../../../docs/padroes-de-dominio.md#3-regras-que-cruzam-agregados): se ele lê dados de um agregado só ou mostra um dos sinais listados lá, leve a lógica para o agregado, a entidade, o value object ou um tipo próprio. Um membro que o plano não previa entra na lista de desvios.

## 4. Quando o plano não fecha com o código

O plano pode errar: um arquivo que não é como ele supôs, uma decisão que o código torna impossível, um caso que ele não previu. Nesses casos, **pergunte no chat, não improvise**:

- diga o que o plano previa, o que o código mostra e as alternativas, com a sua recomendação;
- espere a decisão do dono e implemente como ele disser;
- anote o desvio (o que o plano dizia, o que ficou, por quê e quem decidiu) para o passo 6.

As "Decisões fechadas" do plano valem como decididas: só se reabrem com o dono, nunca por iniciativa da sessão. Desvio pequeno e sem escolha (um nome de arquivo diferente do previsto, um passo dividido em dois commits) não precisa de pergunta, mas entra na lista.

O comentário do plano não é editado: ele registra o que foi combinado antes do código.

## 5. Revisar

Com todos os passos implementados, rode a `revisar-mudanca`. Todo achado, de qualquer severidade, se corrige antes de seguir, a não ser que o dono decida ignorá-lo depois de ouvir o achado e o motivo; se a correção mudar algo do plano, ela também entra na lista de desvios.

## 6. Registrar os desvios do plano

Se houve desvio, publique um comentário na issue implementada (a sub-issue, quando for o caso), começando com `<!-- desvios -->`:

```markdown
<!-- desvios -->
### Desvios do plano

| O plano previa | O que ficou | Por quê | Decidido por |
|---|---|---|---|
| ... | ... | ... | dono do repositório / restrição do código |
```

```bash
gh issue comment <n> --body-file <arquivo-dos-desvios.md>
```

Escreva o arquivo no diretório temporário da sessão. Sem desvio, não há comentário. Quando o desvio veio de um layout visto em `dados/`, descreva só a estrutura (colunas, formatos): o comentário não cita valores, descrições nem nomes de arquivos reais.

## 7. Abrir o PR

Rode a `abrir-pr`. O corpo do PR aponta para o comentário de desvios, quando ele existir.

Depois, se a issue implementada for sub-issue e a pai tiver outra sub-issue aberta e sem dependência pendente, proponha uma sessão nova para ela, do mesmo jeito que a `planejar-issue` propõe a primeira: contexto limpo, prompt `/implementar-issue <m>`, checkout original por padrão e cartão que o dono confirma.
