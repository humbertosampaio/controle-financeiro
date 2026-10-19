// Testes do guardrail: cobrem o falso negativo (o que tem que ser barrado passa)
// e o falso positivo (fluxo normal barrado). O segundo é o que faz alguém desligar o harness.
//
// node scripts/harness/guardrail.test.mjs

import { test } from "node:test";
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { checkCommand, checkWrite } from "./guardrail.mjs";

// Valores com cara de secret montados por concatenação, para este arquivo não disparar a varredura.
const chaveAnthropic = "sk-" + "ant-" + "api03-" + "x".repeat(40);
const tokenGitHub = "gh" + "p_" + "a".repeat(36);
const chavePrivada = "-----BEGIN " + "RSA PRIVATE KEY-----";

const decision = (r) => r?.decision ?? "allow";

const commandCases = [
  // deny
  ["git commit --no-verify -m \"x\"", "deny"],
  ["git commit -n -m \"feat: X\"", "deny"],
  ["git add -f dados/c6.ofx", "deny"],
  ["git add --force .", "deny"],
  ["git add dados/", "deny"],
  ["git add extrato.csv", "deny"],
  ["git add src/Importacao/extrato.ofx", "deny"],
  ["git push origin main", "deny"],
  ["git push origin HEAD:main", "deny"],
  ["git push --force-with-lease origin feat/x:main", "deny"],
  ["dotnet user-secrets list --project src/ControleFinanceiro.AppHost", "deny"],
  [`curl -H "x-api-key: ${chaveAnthropic}" https://api.anthropic.com`, "deny"],
  [`export GH_TOKEN=${tokenGitHub}`, "deny"],
  // ask
  ["git push --force origin feat/1-x", "ask"],
  ["git push --force-with-lease origin feat/1-x", "ask"],
  ["git reset --hard HEAD~1", "ask"],
  ["git clean -fd", "ask"],
  ["git branch -D feat/velha", "ask"],
  ["git checkout -- .", "ask"],
  ["git rebase main", "ask"],
  ["git config core.hooksPath .githooks", "ask"],
  ["rm -rf bin", "ask"],
  ["Remove-Item -Recurse -Force obj", "ask"],
  ["dotnet add package Pluggy.SDK", "ask"],
  ["dotnet ef database drop", "ask"],
  ["dotnet ef migrations remove", "ask"],
  ["docker volume rm postgres-volume", "ask"],
  // allow: fluxo normal
  ["git status", "allow"],
  ["git add src/ControleFinanceiro.Domain/Transacoes/Transacao.cs", "allow"],
  ["git add tests/ControleFinanceiro.Importacao.Tests/Fixtures/c6-extrato.csv", "allow"],
  ["git add -A", "allow"],
  ["git commit -m \"feat(importacao): Lê o extrato OFX do C6\"", "allow"],
  ["git commit -m \"docs: Explica a opção -n do comando\"", "allow"],
  ["git push -u origin HEAD", "allow"],
  ["git push -n origin feat/1-x", "allow"],
  ["git push origin feat/7-importar-ofx-c6", "allow"],
  ["git checkout -b feat/7-importar-ofx-c6", "allow"],
  ["git branch -a", "allow"],
  ["git log --oneline origin/main..HEAD", "allow"],
  ["dotnet build ControleFinanceiro.slnx", "allow"],
  ["dotnet test ControleFinanceiro.slnx", "allow"],
  ["dotnet ef migrations add AdicionaTransacoes", "allow"],
  ["dotnet user-secrets set \"Parameters:PostgresPassword\" \"x\" --project src/ControleFinanceiro.AppHost", "allow"],
  ["head -5 dados/c6-extrato-2026-09.csv", "allow"],
  ["gh issue view 1 --comments", "allow"],
  ["docker ps -a", "allow"],
];

test("comandos de shell", () => {
  for (const [command, expected] of commandCases) {
    assert.equal(decision(checkCommand(command, { branch: "feat/1-x" })), expected, command);
  }
});

test("texto dentro de heredoc não é comando", () => {
  const corpo = "cat > pr.md <<'EOF'\nPara ver o bloqueio, tente `git add -f dados/x.ofx` ou git push origin main.\nEOF\ngh pr create --body-file pr.md";
  assert.equal(decision(checkCommand(corpo, { branch: "feat/1-x" })), "allow");
  const commitComCorpo = "git commit -F - <<'EOF'\ndocs: Explica por que --no-verify é proibido\nEOF";
  assert.equal(decision(checkCommand(commitComCorpo, { branch: "feat/1-x" })), "allow");
  // Um comando de verdade depois do heredoc continua sendo avaliado.
  const depois = "cat > a.txt <<'EOF'\ntexto\nEOF\ngit add -f dados/x.ofx";
  assert.equal(decision(checkCommand(depois, { branch: "feat/1-x" })), "deny");
  // Secret dentro de heredoc continua bloqueado: o heredoc pode estar escrevendo um arquivo.
  assert.equal(decision(checkCommand(`cat > a.cs <<'EOF'\n${chaveAnthropic}\nEOF`, { branch: "feat/1-x" })), "deny");
});

test("push sem refspec depende da branch atual", () => {
  assert.equal(decision(checkCommand("git push", { branch: "main" })), "deny");
  assert.equal(decision(checkCommand("git push origin", { branch: "main" })), "deny");
  assert.equal(decision(checkCommand("git push", { branch: "feat/1-x" })), "allow");
  assert.equal(decision(checkCommand("git push -u origin HEAD", { branch: "feat/1-x" })), "allow");
});

const writeCases = [
  // deny
  ["src/ControleFinanceiro.WebApi/Program.cs", `var key = "${chaveAnthropic}";`, "deny"],
  ["docs/setup.md", `token: ${tokenGitHub}`, "deny"],
  ["deploy/key.pem", chavePrivada, "deny"],
  ["src/ControleFinanceiro.WebApi/appsettings.Development.json", "{\"ConnectionStrings\":{\"db\":\"Host=x;Password=segredo\"}}", "deny"],
  ["dados/c6-extrato-2026-09.csv", "Data;Valor", "deny"],
  ["D:\\Git\\controle-financeiro\\dados\\novo.ofx", "<OFX>", "deny"],
  // ask
  ["src/Importacao/exemplo.csv", "Data;Valor", "ask"],
  ["src/ControleFinanceiro.Infrastructure.Persistence/Migrations/20261018_AdicionaTransacoes.cs", "migrationBuilder.DropTable(\"x\");", "ask"],
  [".claude/settings.json", "{}", "ask"],
  [".githooks/pre-commit", "#!/bin/sh", "ask"],
  ["scripts/harness/guardrail.mjs", "//", "ask"],
  [".github/workflows/ci.yml", "on: push", "ask"],
  [".gitignore", "/dados/", "ask"],
  ["Directory.Packages.props", "<Project/>", "ask"],
  // allow
  ["src/ControleFinanceiro.Domain/Transacoes/Transacao.cs", "public sealed class Transacao {}", "allow"],
  ["tests/ControleFinanceiro.Importacao.Tests/Fixtures/c6-extrato.csv", "Data;Valor\n01/09/2026;10.00", "allow"],
  ["tests\\ControleFinanceiro.Importacao.Tests\\Fixtures\\c6.ofx", "<OFX>", "allow"],
  ["src/ControleFinanceiro.WebApi/appsettings.Development.json", "{\"Logging\":{}}", "allow"],
  ["docs/trabalhando-com-ia.md", "Use `dotnet user-secrets set` para a senha.", "allow"],
  ["CLAUDE.md", "Chave da Anthropic começa com sk-ant-, guardada em User Secrets.", "allow"],
];

test("escrita de arquivos", () => {
  for (const [path, content, expected] of writeCases) {
    assert.equal(decision(checkWrite(path, content)), expected, path);
  }
});

// O pre-push recebe "<local_ref> <local_sha> <remote_ref> <remote_sha>" no stdin.
const rodarPrePush = (linha) => {
  try {
    execFileSync("sh", ["-c", `printf '%s\\n' "$1" | sh .githooks/pre-push --somente-refs`, "_", linha], {
      cwd: fileURLToPath(new URL("../../", import.meta.url)),
      stdio: "pipe",
    });
    return "passa";
  } catch {
    return "bloqueia";
  }
};
const sha = "a".repeat(40);
const zero = "0".repeat(40);

// Com "git push origin HEAD", local_ref chega como "HEAD": o nome precisa vir do remote_ref.
test("pre-push valida a branch pelo nome no remoto", () => {
  assert.equal(rodarPrePush(`HEAD ${sha} refs/heads/chore/1-base-do-repositorio ${zero}`), "passa");
  assert.equal(rodarPrePush(`refs/heads/feat/7-x ${sha} refs/heads/feat/7-x ${sha}`), "passa");
  assert.equal(rodarPrePush(`HEAD ${sha} refs/heads/main ${sha}`), "bloqueia");
  assert.equal(rodarPrePush(`HEAD ${sha} refs/heads/minha-branch ${zero}`), "bloqueia");
});

// Branches do Dependabot não seguem <tipo>/<issue>-<slug>, mas às vezes precisam de um ajuste nosso.
test("pre-push aceita branch do Dependabot", () => {
  const dependabot = "refs/heads/dependabot/docker/src/ControleFinanceiro.AppHost/Resources/library/postgres-18.6";
  assert.equal(rodarPrePush(`HEAD ${sha} ${dependabot} ${sha}`), "passa");
  assert.equal(rodarPrePush(`HEAD ${sha} refs/heads/dependabot ${sha}`), "bloqueia");
  assert.equal(rodarPrePush(`HEAD ${sha} refs/heads/feat/dependabot/x ${sha}`), "bloqueia");
});

test("deny vence ask: secret em arquivo guardado bloqueia", () => {
  assert.equal(decision(checkWrite(".github/workflows/ci.yml", `token: ${tokenGitHub}`)), "deny");
});
