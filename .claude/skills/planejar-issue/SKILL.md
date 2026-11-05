---
name: planejar-issue
description: Transforma uma issue existente do controle-financeiro num plano publicado no GitHub, antes de qualquer código. O plano vai num único comentário da issue, cada etapa grande vira uma sub-issue e, no fim, propõe uma sessão nova para implementar. Use quando o pedido for "planejar a issue N" ou antes de implementar algo não trivial. Não implementa nada.
---

# planejar-issue

Leva uma issue de "problema descrito" a "plano aprovado e publicado". **Não escreve código**: termina com a issue atualizada e a implementação proposta para uma sessão nova.

A implementação nunca acontece na sessão que planejou. Ela começa com o contexto limpo e lê só o que foi publicado, sem as versões descartadas do plano nem as voltas da conversa. Por isso o que vai para o GitHub precisa se bastar sozinho.

| Ferramenta | Quando | Entrega |
|---|---|---|
| agente `planejador` | tarefa não trivial | plano no chat |
| **`planejar-issue`** (esta) | a issue existe e precisa virar execução | plano na issue + sub-issues + sessão de implementação proposta |
| `implementar-issue` | numa sessão nova, com o plano publicado | branch com os commits do plano |
| `revisar-mudanca` | implementação pronta | gates + revisores |
| `abrir-pr` | revisão limpa | push + PR com `Closes #n` |

## 1. Ler a issue inteira

```bash
gh issue view <n> --comments
```

Valide que `<n>` é numérico antes de usá-lo em comando. Leia corpo e comentários: o que já foi decidido não se replaneja por cima. Se já existir um comentário começando com `<!-- plano -->`, é o plano anterior: a revisão parte dele e, no passo 4, edita esse comentário em vez de criar outro.

## 2. Montar o plano

Use o agente `planejador` com o número e o resumo da issue. Revise o que ele devolver:
- cita arquivos reais do repositório?
- a seção de privacidade diz o que sai da máquina?
- cada passo da ordem de execução é verificável e vira um commit semântico?
- com serviço de domínio novo, a tabela de arquivos (e a seção "Arquivos" da sub-issue) lista os membros novos nas entidades e nos value objects, por nome, e não só o arquivo do serviço? O item 9 do checklist do `docs/padroes-de-dominio.md` está respondido?
- cada tipo novo do Domain está na tabela do item 7 do checklist, com a forma decidida pelas perguntas da seção 7 do guia? Um tipo com nome de conceito do produto que ficou `record` "de retorno", primitivo ou tupla pede revisão antes de mostrar o plano.

## 3. Mostrar e esperar aprovação

Mostre o plano no chat e **pare até o dono do repositório aprovar**. Ajuste o que ele pedir. Nada é publicado antes do "pode publicar".

Antes de publicar, consolide a conversa no plano. Ele precisa ser lido por quem não estava nela:
- cada decisão tomada no chat entra numa seção **Decisões fechadas**, com o motivo em uma frase. O que ficar só na conversa não existe para quem implementa;
- nada de "como conversamos", "a opção B" ou referência a uma versão anterior: o plano descreve o desenho final, não o caminho até ele;
- o que continua em aberto fica em "Riscos e decisões pendentes", dizendo quem decide e quando;
- o motivo de cada decisão descreve estrutura e regra, nunca valores, descrições ou nomes vindos de `dados/` ou colados no chat.

## 4. Publicar

O plano vive num **único comentário** da issue, que começa com a linha `<!-- plano -->`. Escreva o arquivo no diretório temporário da sessão, não no repositório.

```bash
id=$(gh api repos/{owner}/{repo}/issues/<n>/comments --paginate --jq '.[] | select(.body | startswith("<!-- plano -->")) | .id' | head -n 1)
if [ -z "$id" ]; then
  gh issue comment <n> --body-file <arquivo-do-plano.md>
else
  gh api repos/{owner}/{repo}/issues/comments/$id -X PATCH -F body=@<arquivo-do-plano.md>
fi
```

Revisão do plano depois de publicado edita esse comentário, nunca cria um segundo. Depois que a implementação começa, ele não muda mais: o que ficar diferente vai para o comentário "Desvios do plano", que a `implementar-issue` publica no fim.

Para cada etapa grande o bastante para ter PR próprio, crie uma sub-issue com as mesmas labels de `fase:*` e `area:*` da pai e o tipo adequado:

```bash
url=$(gh issue create --title "<etapa>" --label "tipo:us,fase:0,area:importacao" --body-file <arquivo-da-etapa.md>)
filho=${url##*/}
id=$(gh api repos/{owner}/{repo}/issues/$filho --jq .id)
gh api repos/{owner}/{repo}/issues/<n>/sub_issues -X POST -F sub_issue_id=$id
```

A sub-issue é o briefing de quem implementa e precisa se bastar sozinha. Use este formato:

```markdown
Plano: #<n> (comentário `<!-- plano -->`)

### Objetivo
<uma frase>

### Arquivos
<caminho | novo ou alterado | o que muda, citando o análogo a seguir>

### Decisões fechadas
<as do plano que valem para esta etapa; não se reabrem sem o dono>

### Passos
1. <passo verificável, que vira um commit>

### Pronto quando
- [ ] <critério verificável>

### Pergunte em vez de improvisar
<o que o plano não resolveu ou que pode não fechar com o código>

### Depende de
<sub-issues que precisam estar na `main` antes, ou "nada">
```

Sem sub-issues (o plano cabe num PR só), o próprio comentário do plano é o briefing e precisa ter as mesmas seções.

Se o repositório tiver um Project, adicione cada sub-issue a ele:

```bash
gh project list --owner humbertosampaio
gh project item-add <numero-do-project> --owner humbertosampaio --url "$url"
```

## 5. Encerrar

Diga o link do comentário do plano e a lista de sub-issues criadas, na ordem de execução, e qual começa primeiro.

## 6. Propor a sessão de implementação

Proponha uma **sessão nova** para a primeira issue a implementar (a primeira sub-issue, ou a própria issue quando não houver sub-issues), como um cartão que o dono do repositório confirma. Não a inicie sem esse ok.

- **Contexto limpo**: a sessão não herda esta conversa (no app desktop, `start_session` com `context: "fresh"`).
- **Prompt mínimo**: `/implementar-issue <m>`. Sem resumo da conversa nem do plano: tudo o que a sessão precisa está publicado, e repetir aqui é justamente o ruído que se quer evitar.
- **Checkout original por padrão** (`use_worktree: false`): o dono acompanha as alterações no Visual Studio, na branch da issue. Worktree só quando ele pedir. Como as duas sessões dividem o checkout, esta não edita arquivos nem troca de branch depois de propor a outra.
- **Uma de cada vez**: a próxima sub-issue é proposta pela própria sessão de implementação, quando ela abrir o PR. Duas em paralelo só quando o plano diz que não dependem uma da outra nem tocam os mesmos arquivos, e então cada uma no seu worktree.

Fora do app desktop, mostre o comando para abrir a sessão:

```bash
claude "/implementar-issue <m>"
```
