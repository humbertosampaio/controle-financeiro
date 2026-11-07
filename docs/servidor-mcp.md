# Servidor MCP

Como registrar o servidor MCP (`src/ControleFinanceiro.Mcp`) no Claude Code e no Claude Desktop e conferir que ele responde. O servidor roda por stdio: não abre porta nem sobe pelo AppHost, quem o inicia é o cliente MCP.

## Pré-requisitos

1. **Postgres de pé**, pelo AppHost:

   ```bash
   dotnet run --project src/ControleFinanceiro.AppHost
   ```

2. **User Secrets do servidor**: a conexão e o usuário cujos dados as ferramentas leem. O servidor os carrega mesmo fora de Development.

   ```bash
   dotnet user-secrets set "ConnectionStrings:controlefinanceiro" "<conexão>" --project src/ControleFinanceiro.Mcp
   dotnet user-secrets set "Mcp:IdUsuario" "<id do usuário>" --project src/ControleFinanceiro.Mcp
   ```

   Variável de ambiente vence o secret, mas a conexão com senha fica só no User Secret, nunca no `env` de um `.mcp.json` ou do `claude_desktop_config.json`. O guardrail e o `pre-commit` barram senha no `.mcp.json`. Sem o `Mcp:IdUsuario`, só o `verificar_servidor` responde.

3. **Servidor publicado** em `artifacts/mcp/` (ignorada pelo git):

   ```bash
   pwsh scripts/PublicarMcp.ps1
   ```

   O cliente executa essa cópia, e não a saída de `bin/`: no Windows o processo do servidor trava a DLL carregada, e um `dotnet build` com uma sessão aberta falharia. O `dotnet run` também não serve, porque escreve a saída do build no stdout, que é do protocolo. Depois de mudar o servidor, feche as sessões que o usam, publique de novo e reconecte.

## Claude Code

O servidor está registrado no projeto, em [`.mcp.json`](../.mcp.json), com o nome `controle-financeiro` e caminho relativo à raiz do repositório. Vale para o `claude` no terminal e para a aba Code do app Claude, desde que a sessão seja aberta na raiz do repositório.

1. Na primeira sessão, o Claude Code pergunta se aprova os servidores do `.mcp.json` do projeto. Aprove o `controle-financeiro`.
2. Um registro antigo de escopo local com o mesmo nome vence o do projeto e o esconde. Confira com `claude mcp get controle-financeiro`: o escopo tem de ser `Project config`. Se aparecer `Local config`, remova o registro local:

   ```bash
   claude mcp remove controle-financeiro -s local
   ```

3. `/mcp` lista o servidor como conectado e reconecta depois de uma nova publicação.

Os logs do servidor (o stderr dele) ficam em `%LOCALAPPDATA%/claude-cli-nodejs/Cache/<projeto>/mcp-logs-controle-financeiro/`, em que `<projeto>` é o caminho do repositório com os separadores trocados por hífen (ex.: `D--Git-controle-financeiro`).

## Claude Desktop

O Claude Desktop (a aba de Chat do app; a aba Code é o Claude Code da seção anterior) não lê o `.mcp.json` do projeto. Registre o servidor no `claude_desktop_config.json` dele, que fica fora do repositório porque traz o caminho absoluto da máquina. Abra o arquivo pelo próprio app, em **Configurações → Desenvolvedor → Editar configuração**: o caminho muda conforme a instalação, e um arquivo no lugar errado é ignorado sem aviso.

| Instalação no Windows | Configuração | Logs |
|---|---|---|
| pela Microsoft Store (MSIX) | `%LOCALAPPDATA%\Claude-Data\<pasta do pacote>\claude_desktop_config.json`, ao lado das `preferences` do app | `%LOCALAPPDATA%\Claude\logs\` |
| pelo instalador comum | `%APPDATA%\Claude\claude_desktop_config.json` | `%APPDATA%\Claude\logs\` |

Na instalação MSIX, um `%APPDATA%\Claude\claude_desktop_config.json` pode existir e não ser lido. Sem o servidor registrado, o chat não tem a ferramenta e pode tentar rodar o comando no próprio ambiente de execução, que não enxerga a máquina.

Acrescente o servidor em `mcpServers`, com o caminho absoluto da DLL publicada e sem `env` de conexão. Escreva o caminho com barras comuns (`D:/...`): no JSON, a contrabarra precisa ser dobrada, e uma simples some ou quebra o arquivo.

```json
{
  "mcpServers": {
    "controle-financeiro": {
      "command": "dotnet",
      "args": ["<raiz do repositório>/artifacts/mcp/ControleFinanceiro.Mcp.dll"]
    }
  }
}
```

Feche o Claude Desktop pela bandeja (fechar a janela não encerra o app) e abra de novo. O servidor aparece no menu de ferramentas da conversa. O log do servidor é o `mcp-server-controle-financeiro.log`, na pasta de logs da tabela.

O Claude Desktop pede permissão a cada chamada de ferramenta, mostra os argumentos e permite aprovar só aquela chamada. Ele não lê a meta nem o `.claude/settings.json` do projeto e oferece "permitir sempre": **nunca dê "permitir sempre" à `confirmar_vinculo`**, ou o vínculo passa a ser gravado sem você ler o resumo. O risco é aceito por ser uma ferramenta de uso pessoal.

## Vínculo em duas etapas

O vínculo de uma saída a uma despesa é gravado em duas chamadas, e só a segunda grava:

1. **`prever_vinculo`** recebe os ids da transação e da despesa, aplica a regra do vínculo em memória e devolve a transação, a despesa, o valor previsto e um `resumo` em uma frase (valor e data da saída, descrição e contraparte, despesa, mês e valor previsto, com os documentos mascarados e sem ids). Nada é gravado, e o cliente não pergunta nada.
2. **`confirmar_vinculo`** recebe o `resumo` da prévia e os mesmos ids. O cliente pede a sua aprovação antes de chamá-la, e o pedido mostra os argumentos, com o resumo primeiro: é o texto que você aprova, e não dois GUIDs. O servidor refaz o vínculo do zero e só grava se o resumo recalculado for o mesmo.

Recusar a aprovação não chama a ferramenta, e nada é gravado. Se algo mudou entre as duas etapas, a confirmação devolve o erro e não grava: a transação já paga outra despesa (`TransacaoJaVinculada`), a despesa já foi paga por outra saída (`DespesaJaPaga`) ou o resumo não confere (`ResumoDivergente`, que manda pedir a prévia de novo). Confirmar duas vezes devolve `TransacaoJaVinculada`. O vínculo gravado se desfaz pelo endpoint de desvínculo da API.

Quem pede a aprovação é o cliente, por duas travas na `confirmar_vinculo` (detalhes em [harness.md](harness.md#ferramentas-do-servidor-mcp-que-gravam)):

| Cliente | O que faz ele perguntar |
|---|---|
| `claude` no terminal | a meta `anthropic/requiresUserInteraction` da ferramenta, em todos os modos, sem "não perguntar de novo" |
| Claude Code do app desktop | a regra `ask` do `.claude/settings.json` do projeto: no modo auto, ele ignora a meta |
| Claude Desktop | o pedido de permissão de toda chamada; nunca dê "permitir sempre" à `confirmar_vinculo` |

Em `dontAsk` e com `--permission-prompt-tool`, o Claude Code nega a chamada, e o vínculo não acontece.

**Cliente novo.** A trava humana é do host, não do servidor: o servidor só declara a meta e confere o resumo. Antes de registrar o servidor num cliente novo (um bot de WhatsApp, Telegram ou Discord, por exemplo), confira como ele confirma: ele precisa honrar a meta com uma confirmação que não passe pelo modelo, como um botão do chat tratado pelo próprio bot, e não o modelo interpretando a resposta do usuário.

## Roteiro de conferência

Faça as perguntas em ordem, numa conversa nova, em cada cliente. Troque `<mês>` por um mês com transações importadas.

| Pergunta | Ferramenta esperada | O que conferir |
|---|---|---|
| "O servidor do controle financeiro está de pé?" | `verificar_servidor` | banco `disponivel` |
| "Quais transações tive em `<mês>`?" | `listar_transacoes` | as transações das contas e dos cartões, com data, valor, descrição e contraparte |
| "Quais despesas fixas ainda estão pendentes em `<mês>`?" | `listar_despesas_fixas` | as despesas fixas com o status do mês (Pago ou Pendente) |
| "Como está o balanço de `<mês>`?" | `balanco_mensal` | renda líquida, total pago, total pendente e saldo previsto |
| "Quais saídas de `<mês>` parecem pagar as despesas pendentes?" | `listar_transacoes` e `listar_despesas_fixas` | sugestões de pares transação e despesa, sem gravar nada |
| "Vincule a saída `<saída>` à despesa `<despesa>`", e recuse o pedido de aprovação | `prever_vinculo` e o pedido da `confirmar_vinculo` | o resumo nos argumentos do pedido; recusado, a despesa continua pendente |
| A mesma pergunta, aceitando o pedido | `prever_vinculo` e `confirmar_vinculo` | o pedido aparece também no modo auto; aceito, a despesa fica paga pela saída |

`banco indisponivel` no primeiro passo indica o Postgres parado ou a conexão errada no User Secret; uma ferramenta de leitura que falha com o banco disponível indica o `Mcp:IdUsuario` ausente.
