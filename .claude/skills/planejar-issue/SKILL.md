---
name: planejar-issue
description: Transforma uma issue existente do controle-financeiro num plano publicado no GitHub, antes de qualquer código. O plano vai num comentário da issue e cada etapa grande vira uma sub-issue. Use quando o pedido for "planejar a issue N" ou antes de implementar algo não trivial. Não implementa nada.
---

# planejar-issue

Leva uma issue de "problema descrito" a "plano aprovado e publicado". **Não escreve código**: termina com a issue atualizada e a implementação ainda por começar.

| Ferramenta | Quando | Entrega |
|---|---|---|
| agente `planejador` | tarefa não trivial | plano no chat |
| **`planejar-issue`** (esta) | a issue existe e precisa virar execução | plano na issue + sub-issues |
| `revisar-mudanca` | implementação pronta | gates + revisores |
| `abrir-pr` | revisão limpa | push + PR com `Closes #n` |

## 1. Ler a issue inteira

```bash
gh issue view <n> --comments
```

Valide que `<n>` é numérico antes de usá-lo em comando. Leia corpo e comentários: o que já foi decidido não se replaneja por cima.

## 2. Montar o plano

Use o agente `planejador` com o número e o resumo da issue. Revise o que ele devolver:
- cita arquivos reais do repositório?
- a seção de privacidade diz o que sai da máquina?
- cada passo da ordem de execução é verificável e vira um commit semântico?

## 3. Mostrar e esperar aprovação

Mostre o plano no chat e **pare até o dono do repositório aprovar**. Ajuste o que ele pedir. Nada é publicado antes do "pode publicar".

## 4. Publicar

Plano no comentário da issue:

```bash
gh issue comment <n> --body-file <arquivo-do-plano.md>
```

Escreva o arquivo do plano no diretório temporário da sessão, não no repositório.

Para cada etapa grande o bastante para ter PR próprio, crie uma sub-issue com as mesmas labels de `fase:*` e `area:*` da pai e o tipo adequado:

```bash
url=$(gh issue create --title "<etapa>" --label "tipo:us,fase:0,area:importacao" --body-file <arquivo-da-etapa.md>)
filho=${url##*/}
id=$(gh api repos/{owner}/{repo}/issues/$filho --jq .id)
gh api repos/{owner}/{repo}/issues/<n>/sub_issues -X POST -F sub_issue_id=$id
```

Cada sub-issue traz: objetivo em uma frase, arquivos a tocar, critério de pronto e o trecho do plano que a cobre.

Se o repositório tiver um Project, adicione cada sub-issue a ele:

```bash
gh project list --owner humbertosampaio
gh project item-add <numero-do-project> --owner humbertosampaio --url "$url"
```

## 5. Encerrar

Diga o link do comentário do plano e a lista de sub-issues criadas, na ordem de execução, e qual começa primeiro.
