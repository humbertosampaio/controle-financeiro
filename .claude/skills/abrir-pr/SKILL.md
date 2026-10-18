---
name: abrir-pr
description: Publica a branch do controle-financeiro e abre o PR vinculado à issue (Closes #n) no GitHub. Use só depois que revisar-mudanca passou sem achado crítico ou alto.
---

# abrir-pr

Leva a branch revisada até o PR aberto e vinculado.

## 1. Pré-condições

- Está numa branch `<tipo>/<n>-<slug>`, não na `main`.
- `revisar-mudanca` rodou depois do último commit, sem achado CRÍTICO ou ALTO pendente.
- Working tree limpo (`git status --short` vazio). Se houver mudança solta, pergunte se ela entra (num commit semântico próprio) ou fica de fora.

Extraia o número da issue do nome da branch. Se a branch não tiver número, **pergunte** qual issue o PR fecha.

## 2. Conferir os commits

```bash
git log origin/main..HEAD --format='%h %s'
```

Mostre a lista. Cada commit é um bloco coeso em Conventional Commits PT-BR. Se algum misturar assuntos, proponha a divisão antes de publicar.

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
- `Closes #<n>` numa linha própria.
- Os itens do checklist do template ficam **desmarcados**: quem marca é o dono do repositório, depois de ler o diff.

## 5. Encerrar

Mostre o link do PR e confira que a issue aparece vinculada:

```bash
gh pr view --json closingIssuesReferences --jq '.closingIssuesReferences[].number'
```

Se vier vazio, o `Closes #n` não foi reconhecido: corrija o corpo com `gh pr edit`.
