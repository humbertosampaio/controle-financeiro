#!/bin/sh
# Varre arquivos em busca de dados financeiros reais e de secrets.
#
#   sh .githooks/scan-staged.sh                    -> arquivos no stage (usado pelo pre-commit)
#   sh .githooks/scan-staged.sh --branch origin/main -> arquivos alterados na branch (usado pela revisão)
#
# Sem dependência externa: só git, grep e sh, que vêm com o Git for Windows.

mode="staged"
base=""
if [ "$1" = "--branch" ]; then
  mode="branch"
  base="${2:-origin/main}"
fi

if [ "$mode" = "staged" ]; then
  files=$(git diff --cached --name-only --diff-filter=ACMR)
else
  files=$(git diff --name-only --diff-filter=ACMR "$base"...HEAD)
fi

[ -z "$files" ] && exit 0

content_of() {
  if [ "$mode" = "staged" ]; then git show ":$1" 2>/dev/null; else cat "$1" 2>/dev/null; fi
}

falhou=0
erro() {
  echo "  ✗ $1" >&2
  falhou=1
}

IFS='
'
for f in $files; do
  case "$f" in
    dados/*|*/dados/*)
      erro "$f: arquivo de dados/ (extratos reais nunca entram no git)"
      continue ;;
  esac

  case "$f" in
    tests/*/Fixtures/*) ;;
    *.ofx|*.OFX|*.csv|*.CSV|*.xlsx|*.XLSX|*.db|*.sqlite)
      erro "$f: extrato ou banco fora de tests/**/Fixtures/"
      continue ;;
  esac

  texto=$(content_of "$f")
  echo "$texto" | grep -qE 'sk-ant-[A-Za-z0-9_-]{20,}' && erro "$f: parece conter uma chave da Anthropic"
  echo "$texto" | grep -qE 'gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}' && erro "$f: parece conter um token do GitHub"
  echo "$texto" | grep -qE -- '-----BEGIN [A-Z ]*PRIVATE KEY-----' && erro "$f: parece conter uma chave privada"
  case "$f" in
    *appsettings*.json)
      echo "$texto" | grep -qiE 'Password[[:space:]]*=[[:space:]]*[^;"[:space:]]+' && erro "$f: senha em appsettings (use User Secrets)" ;;
  esac
done

if [ "$falhou" -ne 0 ]; then
  echo "" >&2
  echo "Bloqueado: dado financeiro real ou secret indo para o git." >&2
  echo "Tire o arquivo do stage (git restore --staged <arquivo>) e mova o segredo para User Secrets." >&2
  exit 1
fi
exit 0
