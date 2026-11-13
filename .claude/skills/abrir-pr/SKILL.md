---
name: abrir-pr
description: Publica a branch do controle-financeiro e abre o PR vinculado à issue (Closes #n) no GitHub. Use só depois que revisar-mudanca passou e todo achado foi corrigido ou dispensado pelo dono.
---

# abrir-pr

Leva a branch revisada até o PR aberto e vinculado.

## 1. Pré-condições

- Está numa branch `<tipo>/<n>-<slug>`, não na `main`.
- `revisar-mudanca` rodou depois do último commit, e todo achado, de qualquer severidade, foi corrigido ou o dono decidiu ignorá-lo depois de ouvir o achado e o motivo.
- Working tree limpo (`git status --short` vazio). Se houver mudança solta, pergunte se ela entra (num commit semântico próprio) ou fica de fora.

Extraia o número da issue do nome da branch. Se a branch não tiver número, **pergunte** qual issue o PR fecha.

## 2. Conferir os commits

```bash
git log origin/main..HEAD --format='%h %s'
```

Mostre a lista. Cada commit é um bloco coeso em Conventional Commits PT-BR. Se algum misturar assuntos, proponha a divisão antes de publicar.

O merge na `main` é com merge commit, sem squash: estes commits entram na `main` exatamente como estão. Por isso, um commit que só corrige algo criado na própria branch (ex.: `fix(hooks)` sobre um hook adicionado dois commits antes) deve ser dobrado no commit original antes do push:

```bash
git commit --fixup <sha-do-commit-original>
GIT_SEQUENCE_EDITOR=true git rebase -i --autosquash origin/main
```

O rebase pede confirmação ao guardrail. Se a branch já foi publicada, o push depois disso precisa de `--force-with-lease`, que também pede confirmação. Correções que respondem à revisão de um PR já aberto entram como commits normais.

## 3. Push

```bash
git push -u origin HEAD
```

## 4. PR

Título curto (até ~70 caracteres), no mesmo formato de commit. Corpo pelo template `.github/pull_request_template.md`:

```bash
gh pr create --base main --title "<tipo(escopo): Descrição>" --body-file <arquivo-do-corpo.md>
```

Escreva o corpo no diretório temporário da sessão. Ele leva:
- **O que muda e por quê**, em 1 a 3 frases, sem listar arquivos.
- **Como verificar**, com comandos.
- **Privacidade**: o que passou a sair da máquina, ou "nada".
- **Desvios do plano**: o link do comentário `<!-- desvios -->` da issue, quando a `implementar-issue` tiver publicado um, para quem revisa saber onde o código diverge do plano de propósito:
  ```bash
  gh api repos/{owner}/{repo}/issues/<n>/comments --paginate --jq '.[] | select(.body | startswith("<!-- desvios -->")) | .html_url'
  ```
- `Closes #<n>` numa linha própria.
- Os itens do checklist do template ficam **desmarcados**: quem marca é o dono do repositório, depois de ler o diff.

## 5. Encerrar

Mostre o link do PR e confira que a issue aparece vinculada:

```bash
gh pr view --json closingIssuesReferences --jq '.closingIssuesReferences[].number'
```

Se vier vazio, o `Closes #n` não foi reconhecido: corrija o corpo com `gh pr edit`.

## 6. Esperar o CI e fazer o merge

O dono do repositório é o único que trabalha nele e revisa cada commit antes de ele ser feito (a aprovação do comando de commit), então não revisa o PR: a própria sessão espera o CI e faz o merge, sem pedir ok. O auto-merge do GitHub está desligado no repositório; por isso a espera é feita pela sessão, em segundo plano:

```bash
gh pr checks <numero> --watch --interval 30
```

- **CI verde:** merge com merge commit (squash e rebase estão desligados no repositório). O `--delete-branch` apaga a branch remota:
  ```bash
  gh pr merge <numero> --merge --delete-branch
  ```
  Depois, limpe o clone. O `gh` tenta trocar para a `main` e apagar a branch local, mas falha em silêncio quando a sessão roda num worktree, e não limpa as refs de branches remotas já apagadas:
  ```bash
  git fetch --prune origin
  git switch main && git pull --ff-only   # num worktree: git switch --detach origin/main
  git branch -d <branch-do-pr>            # se ainda existir; -d só apaga o que já está mergeado
  git branch --merged origin/main         # deve listar só a main
  ```
  Se a última lista trouxer outras branches, são sobras de PRs anteriores: mostre-as ao dono e apague com `git branch -d` só depois do ok dele. Nunca use `-D`.
- **CI vermelho:** corrija na branch, com commit normal (o PR já está aberto), faça o push e espere de novo.
- **Conflito com a `main`, ou um comentário de revisão no PR:** pare e pergunte ao dono antes do merge.

Ao fim, confira que a issue foi fechada (`gh issue view <n> --json state`).
