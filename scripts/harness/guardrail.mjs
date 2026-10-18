// Guardrail do Claude Code: hook PreToolUse em Bash, PowerShell, Write, Edit e NotebookEdit.
//
// Decide entre três respostas:
//   deny  -> vazamento de dado financeiro ou secret, ou contorno dos gates. O agente não executa.
//   ask   -> operação guardada (destrutiva, dependência, migration, harness). O humano confirma.
//   allow -> nada a dizer; segue o fluxo normal de permissão (o script não imprime nada).
//
// deny sempre vence ask. Erro inesperado aqui devolve allow (falha aberta): um bug no guardrail
// não pode travar a sessão. Por isso ele tem testes próprios em guardrail.test.mjs.

import { execFileSync } from "node:child_process";
import { pathToFileURL } from "node:url";

const SECRET_PATTERNS = [
  { re: /sk-ant-[A-Za-z0-9_-]{20,}/, nome: "chave da Anthropic" },
  { re: /gh[pousr]_[A-Za-z0-9]{30,}/, nome: "token do GitHub" },
  { re: /github_pat_[A-Za-z0-9_]{30,}/, nome: "token do GitHub" },
  { re: /-----BEGIN [A-Z ]*PRIVATE KEY-----/, nome: "chave privada" },
];

const DATA_EXTENSIONS = /\.(ofx|csv|xlsx|db|sqlite)$/i;
const FIXTURES = /(^|[\\/])tests[\\/].*[\\/]Fixtures[\\/]/i;
const DADOS_DIR = /(^|[\\/])dados([\\/]|$)/i;

const deny = (motivo) => ({ decision: "deny", motivo });
const ask = (motivo) => ({ decision: "ask", motivo });

function normalizePath(p) {
  return (p ?? "").replaceAll("\\", "/");
}

function isRealDataPath(p) {
  const path = normalizePath(p);
  if (DADOS_DIR.test(path)) return true;
  return DATA_EXTENSIONS.test(path) && !FIXTURES.test(path);
}

function findSecret(text) {
  if (!text) return null;
  for (const { re, nome } of SECRET_PATTERNS) {
    if (re.test(text)) return nome;
  }
  return null;
}

function currentBranch(cwd) {
  try {
    return execFileSync("git", ["rev-parse", "--abbrev-ref", "HEAD"], {
      cwd: cwd || process.cwd(),
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
    }).trim();
  } catch {
    return null;
  }
}

// Corpo de heredoc (<<EOF ... EOF) é texto, não comando: mensagem de commit, corpo de PR, arquivo.
// Sem isso, um PR que explica "git add -f" seria barrado como se o rodasse.
function stripHeredocs(command) {
  return command.replace(/<<-?\s*(['"]?)([A-Za-z_][A-Za-z0-9_]*)\1[^\n]*\n[\s\S]*?\n[ \t]*\2[ \t]*(?=\n|$)/g, "<<heredoc");
}

// Comandos de shell (Bash e PowerShell).
export function checkCommand(command, { branch } = {}) {
  // Secret vale no comando inteiro, heredoc incluído: o heredoc pode estar escrevendo um arquivo.
  const secret = findSecret(command ?? "");
  const cmd = stripHeredocs(command ?? "");
  if (secret) {
    return deny(`O comando contém o que parece ser um(a) ${secret}. Secrets ficam em User Secrets e nunca passam por comando que fica no histórico.`);
  }

  if (/--no-verify\b/.test(cmd) || /\bgit\s+commit\b[^|;&"']*\s-n\b/.test(cmd)) {
    return deny("--no-verify pula os git hooks (varredura de secrets e de dados, Conventional Commits). Corrija o que o hook apontou em vez de contorná-lo.");
  }

  if (/\bgit\s+add\b[^|;&]*\s(-f|--force)\b/.test(cmd)) {
    return deny("git add --force ignora o .gitignore, que é o que mantém dados financeiros reais fora do git. Se o arquivo deve ser versionado, ajuste o .gitignore num commit próprio e explique no PR.");
  }

  const addArgs = cmd.match(/\bgit\s+add\b([^|;&]*)/);
  if (addArgs && addArgs[1].split(/\s+/).some((arg) => arg && !arg.startsWith("-") && isRealDataPath(arg))) {
    return deny("Esse git add inclui dados/ ou um extrato fora de tests/**/Fixtures/. Dados financeiros reais nunca entram no git.");
  }

  if (/\bdotnet\s+user-secrets\s+list\b/.test(cmd)) {
    return deny("dotnet user-secrets list imprime os secrets no transcript. Para conferir uma chave, verifique só se ela existe, sem mostrar o valor.");
  }

  const push = cmd.match(/\bgit\s+push\b([^|;&]*)/);
  if (push) {
    const args = push[1];
    if (/(^|\s|:)(refs\/heads\/)?main(\s|$)/.test(args)) {
      return deny("Push na main não é permitido: tudo entra por PR (skill abrir-pr).");
    }
    const refspecs = args.split(/\s+/).filter((a) => a && !a.startsWith("-"));
    if (refspecs.length <= 1 && branch === "main") {
      return deny("Você está na main e esse push a atualizaria. Crie uma branch <tipo>/<issue>-<slug> e abra um PR.");
    }
    if (/\s(--force|-f)(\s|$)|\s--force-with-lease\b|\s\+\S/.test(` ${args}`)) {
      return ask("Push forçado reescreve o histórico remoto. Confirme que é isso mesmo.");
    }
  }

  const guarded = [
    [/\bgit\s+reset\b[^|;&]*--hard\b/, "git reset --hard descarta mudanças locais."],
    [/\bgit\s+clean\b[^|;&]*\s-[a-zA-Z]*f/, "git clean -f apaga arquivos não versionados (inclusive dados/)."],
    [/\bgit\s+branch\b[^|;&]*\s(-D|-d|--delete|-M|-m)\b/, "Apagar ou renomear branch."],
    [/\bgit\s+(checkout|restore)\b[^|;&]*\s(--\s+)?\.(\s|$)/, "Descarta mudanças do working tree."],
    [/\bgit\s+(rebase|cherry-pick)\b/, "Reescreve commits."],
    [/\bgit\s+config\b[^|;&]*core\.hooksPath/, "Muda onde os git hooks rodam."],
    [/\brm\s+-[a-zA-Z]*r[a-zA-Z]*f|\brm\s+-[a-zA-Z]*f[a-zA-Z]*r|\bRemove-Item\b[^|;&]*-Recurse/, "Remoção recursiva de arquivos."],
    [/\bdotnet\s+(add|remove)\b[^|;&]*\bpackage\b/, "Dependência nova ou removida: justifique (manutenção e cadeia de suprimentos)."],
    [/\bdotnet\s+ef\s+(database\s+(drop|update)|migrations\s+remove)\b/, "Operação de banco ou migration com efeito no banco local, que tem dados reais."],
    [/\bdocker\s+(volume\s+rm|rm\b|system\s+prune)/, "Apaga container ou volume (o Postgres local guarda dados reais)."],
  ];
  for (const [re, motivo] of guarded) {
    if (re.test(cmd)) return ask(motivo);
  }

  return null;
}

// Escrita de arquivo (Write, Edit, NotebookEdit).
export function checkWrite(filePath, content) {
  const path = normalizePath(filePath);

  const secret = findSecret(content);
  if (secret) {
    return deny(`O conteúdo contém o que parece ser um(a) ${secret}. Use User Secrets; em teste, monte o valor por concatenação.`);
  }

  if (/appsettings[^/]*\.json$/i.test(path) && /Password\s*=\s*[^;"\s]+/i.test(content ?? "")) {
    return deny("Senha em appsettings. Connection string com senha vai para User Secrets (dotnet user-secrets set).");
  }

  if (DADOS_DIR.test(path)) {
    return deny("dados/ guarda os arquivos reais do dono do repositório. O agente lê para entender o layout, mas não escreve ali.");
  }

  if (DATA_EXTENSIONS.test(path) && !FIXTURES.test(path)) {
    return ask("Arquivo de extrato fora de tests/**/Fixtures/. Se é fixture, mova para lá; se é dado real, não deve ser criado pelo agente.");
  }

  const guarded = [
    [/\/Migrations\/[^/]+\.cs$/, "Edição de migration: confira se não há operação destrutiva."],
    [/(^|\/)\.claude\/settings[^/]*\.json$/, "Muda o harness (hooks e permissões)."],
    [/(^|\/)\.githooks\//, "Muda os git hooks."],
    [/(^|\/)scripts\/harness\//, "Muda o guardrail."],
    [/(^|\/)\.github\/workflows\//, "Muda o CI."],
    [/(^|\/)\.gitignore$/, "Muda o que fica fora do git (inclusive dados financeiros)."],
    [/(^|\/)Directory\.Packages\.props$/, "Muda versões de pacotes."],
  ];
  for (const [re, motivo] of guarded) {
    if (re.test(path)) return ask(motivo);
  }

  return null;
}

export function evaluate(input) {
  const tool = input?.tool_name ?? "";
  const ti = input?.tool_input ?? {};

  if (tool === "Bash" || tool === "PowerShell") {
    return checkCommand(ti.command, { branch: currentBranch(input?.cwd) });
  }
  if (tool === "Write") return checkWrite(ti.file_path, ti.content);
  if (tool === "Edit") return checkWrite(ti.file_path, ti.new_string);
  if (tool === "NotebookEdit") return checkWrite(ti.notebook_path, ti.new_source);
  return null;
}

function main() {
  let raw = "";
  process.stdin.setEncoding("utf8");
  process.stdin.on("data", (chunk) => (raw += chunk));
  process.stdin.on("end", () => {
    try {
      const result = evaluate(JSON.parse(raw));
      if (!result) return;
      process.stdout.write(
        JSON.stringify({
          hookSpecificOutput: {
            hookEventName: "PreToolUse",
            permissionDecision: result.decision,
            permissionDecisionReason: `[guardrail] ${result.motivo}`,
          },
        }),
      );
    } catch {
      // Falha aberta: sem saída, o fluxo normal de permissão decide.
    }
  });
}

if (import.meta.url === pathToFileURL(process.argv[1] ?? "").href) main();
