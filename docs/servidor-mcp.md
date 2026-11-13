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

O stderr do servidor fica em `%LOCALAPPDATA%/claude-cli-nodejs/Cache/<projeto>/mcp-logs-controle-financeiro/`, em que `<projeto>` é o caminho do repositório com os separadores trocados por hífen (ex.: `D--Git-controle-financeiro`). O Claude Code só o grava quando o processo cai ou reinicia: a exceção de uma ferramenta fica no [log do servidor](#log-do-servidor).

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

Feche o Claude Desktop pela bandeja (fechar a janela não encerra o app) e abra de novo. O servidor aparece no menu de ferramentas da conversa. O stderr do servidor vai para o `mcp-server-controle-financeiro.log`, na pasta de logs da tabela, e o servidor também grava o [log dele](#log-do-servidor).

O Claude Desktop pede permissão a cada chamada de ferramenta, mostra os argumentos e permite aprovar só aquela chamada. Ele não lê a meta nem o `.claude/settings.json` do projeto e oferece "permitir sempre": **nunca dê "permitir sempre" à `confirmar_vinculo`, à `confirmar_pagamento_fatura` nem à `confirmar_importacao`**, ou o vínculo, o pagamento e a importação passam a ser gravados sem você ler o resumo. O risco é aceito por ser uma ferramenta de uso pessoal.

## Log do servidor

O servidor grava o próprio log com o Serilog, configurado pelo [`appsettings.json`](../src/ControleFinanceiro.Mcp/appsettings.json) do projeto, em dois lugares:

- **no arquivo** `artifacts/mcp/logs/mcp-AAAAMMDD.log`, ao lado da DLL publicada, seja qual for o diretório de trabalho do cliente. Ele vira um arquivo novo a cada dia e a cada 10 MB, e só os 14 mais recentes ficam. O Claude Code e o Claude Desktop podem subir a mesma cópia ao mesmo tempo e dividem o arquivo: as linhas "Application started" e "Application is shutting down" separam os processos;
- **no stderr**, que vai para o log do cliente (seções acima). O stdout é do protocolo e não recebe log.

Fica registrado o que for `Warning` ou mais grave, além do início e da parada do processo. A exceção de uma ferramenta aparece em `Error`, com o tipo e a pilha, enquanto o modelo recebe só "An error occurred invoking '<ferramenta>'.". Uma falha na partida, como a connection string ausente, aparece em `Fatal`.

Para ver mais detalhes, suba o nível por variável de ambiente, só durante a investigação, e nunca no `appsettings.json` versionado. Por exemplo, num terminal na raiz do repositório:

```powershell
$env:Serilog__MinimumLevel__Override__ModelContextProtocol = 'Debug'; claude
```

A variável continua no terminal depois que o `claude` fecha: remova-a com `Remove-Item Env:Serilog__MinimumLevel__Override__ModelContextProtocol` ou feche o terminal.

Em `Debug` ou `Verbose` (o Serilog não tem `Trace`), o SDK pode registrar as mensagens JSON-RPC inteiras, com os argumentos e as respostas das ferramentas, ou seja, as transações que o modelo viu. Elas vão para o arquivo e para o stderr que o cliente guarda. Ao terminar a investigação, remova a variável e apague os arquivos de `artifacts/mcp/logs/` gravados durante ela.

O arquivo pode trazer dados do arquivo importado (a mensagem de uma exceção do leitor cita o registro), além do host e do nome do banco. Ele fica só na máquina: o `.gitignore` o ignora, o `pre-commit` barra um `*.log` ou uma pasta `logs/` no stage, e nenhuma ferramenta o lê. Pode ser apagado à vontade.

## Vínculo em duas etapas

O vínculo de uma saída a uma despesa é gravado em duas chamadas, e só a segunda grava:

1. **`prever_vinculo`** recebe os ids da transação e da despesa, aplica a regra do vínculo em memória e devolve a transação, a despesa, o valor previsto e um `resumo` em uma frase (valor e data da saída, descrição e contraparte, despesa, mês e valor previsto, com os documentos mascarados e sem ids). Nada é gravado, e o cliente não pergunta nada.
2. **`confirmar_vinculo`** recebe o `resumo` da prévia e os mesmos ids. O cliente pede a sua aprovação antes de chamá-la, e o pedido mostra os argumentos, com o resumo primeiro: é o texto que você aprova, e não dois GUIDs. O servidor refaz o vínculo do zero e só grava se o resumo recalculado for o mesmo.

Recusar a aprovação não chama a ferramenta, e nada é gravado. Se algo mudou entre as duas etapas, a confirmação devolve o erro e não grava: a transação já paga outra despesa (`TransacaoJaVinculada`), a despesa já foi paga por outra saída (`DespesaJaPaga`) ou o resumo não confere (`ResumoDivergente`, que manda pedir a prévia de novo). Confirmar duas vezes devolve `TransacaoJaVinculada`. O vínculo gravado se desfaz pelo endpoint de desvínculo da API.

Quem pede a aprovação é o cliente, por duas travas na `confirmar_vinculo`, na `confirmar_pagamento_fatura` e na `confirmar_importacao` (detalhes em [harness.md](harness.md#ferramentas-do-servidor-mcp-que-gravam)):

| Cliente | O que faz ele perguntar |
|---|---|
| `claude` no terminal | a meta `anthropic/requiresUserInteraction` da ferramenta, em todos os modos, sem "não perguntar de novo" |
| Claude Code do app desktop | a regra `ask` do `.claude/settings.json` do projeto: no modo auto, ele ignora a meta |
| Claude Desktop | o pedido de permissão de toda chamada; nunca dê "permitir sempre" a uma ferramenta `confirmar_*` |

Em `dontAsk` e com `--permission-prompt-tool`, o Claude Code nega a chamada, e o vínculo não acontece.

## Pagamento da fatura em duas etapas

A saída da conta que pagou a fatura do cartão paga, de uma vez, as despesas já cobradas e pendentes de todos os cartões da mesma fatura (mesmo banco e mesmos dias de fechamento e de vencimento, como os finais do C6) no mês em que ela vence, com o valor cobrado. Como no vínculo, só a segunda chamada grava, e com as mesmas travas de aprovação:

1. **`listar_cartoes`** devolve os cartões, com id, banco, final e dias, para o modelo achar o cartão da fatura. Qualquer cartão da mesma fatura serve.
2. **`prever_pagamento_fatura`** recebe o id da saída, o id do cartão e o ano e o mês do vencimento, aplica o pagamento em memória e devolve a saída, as despesas pagas, o total e um `resumo` em uma frase (valor e data da saída, descrição e contraparte, banco, final do cartão informado, mês de vencimento, quantidade de despesas e total, com os documentos mascarados e sem ids). Nada é gravado.
3. **`confirmar_pagamento_fatura`** recebe o `resumo` da prévia, primeiro nos argumentos, e os mesmos ids e mês. O servidor refaz o pagamento do zero e só grava se o resumo recalculado for o mesmo.

Uma cobrança que chegou entre as duas etapas muda o total, e a confirmação devolve `ResumoDivergente` sem gravar. Sem nada cobrado e pendente no mês, inclusive ao confirmar duas vezes, o erro é `FaturaSemCobrancaPendente`, que costuma indicar o mês ou o cartão errado; a saída que já pagou outra fatura devolve `TransacaoPagaOutraFatura`. O pagamento gravado se desfaz pelo endpoint de desfazer o pagamento da fatura da API.

**Cliente novo.** A trava humana é do host, não do servidor: o servidor só declara a meta e confere o resumo. Antes de registrar o servidor num cliente novo (um bot de WhatsApp, Telegram ou Discord, por exemplo), confira como ele confirma: ele precisa honrar a meta com uma confirmação que não passe pelo modelo, como um botão do chat tratado pelo próprio bot, e não o modelo interpretando a resposta do usuário.

## Importação em duas etapas

O servidor importa os extratos e as faturas que estão na pasta `dados/` do repositório, ignorada pelo git, com a mesma regra da importação pela API. Como no vínculo, só a segunda chamada grava, e com as mesmas travas de aprovação:

1. **`prever_importacao`** recebe o `caminho` do arquivo, o `layout` e, só no `mercado-pago-conta-csv`, o `sufixoConta` que você informa (os 4 últimos caracteres do número da conta, que esse arquivo não traz). O servidor lê o arquivo, aplica a importação em memória e devolve o banco, o período, os lançamentos lidos, novos e já existentes, os cartões da fatura, se a conta será criada e um `resumo` em uma frase. Nada é gravado.
2. **`confirmar_importacao`** recebe o `resumo` da prévia, primeiro nos argumentos, e os mesmos caminho, layout e sufixo. O servidor lê o arquivo de novo, refaz a importação do zero e só grava se o resumo recalculado for o mesmo. O resumo traz o final da conta, e outro sufixo na confirmação muda o resumo e não grava. O caminho não entra no resumo, mas fica nos argumentos do pedido de aprovação.

| Layout | Arquivo |
|---|---|
| `c6-conta-csv` | extrato CSV da conta do C6 |
| `mercado-pago-conta-csv` | extrato CSV da conta do Mercado Pago, com o `sufixoConta` |
| `c6-cartao-csv` | fatura CSV do cartão do C6 |
| `mercado-pago-cartao-csv` | fatura CSV do cartão do Mercado Pago, no formato de [fatura-mercado-pago.md](fatura-mercado-pago.md) |

**O que vai ao modelo.** Só contagens, as datas do período, o banco, o layout, os finais de cartão, o final da conta (no resumo), o número da linha com erro e mensagens fixas. Nenhuma linha do arquivo, nem a descrição, a contraparte ou o valor de um lançamento, nem o nome do arquivo, o número inteiro da conta ou os ids. A falha de leitura diz só a linha e o tipo (fora do layout, sem as colunas, lançamento inválido ou conteúdo inválido); para ver o motivo completo, importe o mesmo arquivo pela API.

**Pasta de origem.** Por padrão, é a `dados/` duas pastas acima da cópia publicada: de `artifacts/mcp/`, a `dados/` da raiz do repositório. Para usar outra pasta, defina `Mcp:OrigemArquivoImportacao` no User Secret do servidor ou numa variável de ambiente. O caminho pode ser absoluto ou relativo à pasta que contém a pasta de origem, que, com a pasta padrão, é a raiz do repositório: `dados/2026/extrato.csv`. O servidor só abre um `.csv` de até 5 MB dentro dessa pasta, e recusa o caminho que sai dela por `..`, por uma pasta irmã com o mesmo prefixo ou por um link (symlink ou junction) no caminho. Fora do `artifacts/mcp/`, como no `bin/` do `src` ou dos testes, a pasta padrão não existe e a importação falha: o servidor nunca procura o repositório subindo pastas.

**Cadastros que faltam.** A conta do extrato que ainda não existe é criada, e a prévia avisa (`conta_nova`). O cartão nunca é criado: a fatura com um final sem cartão é recusada (`CartoesNaoResolvidos`) e lista os finais em `pendencias.finais_sem_cartao`. Os textos de beneficiário e as categorias do banco que ainda não são alias também recusam a importação, e vão só como quantidade em `pendencias`. Nos três casos, resolva pela API (a prévia da importação na API lista os textos) e importe de novo.

**Erros.** Confirmar duas vezes, ou confirmar depois que o arquivo mudou, devolve `ResumoDivergente` sem gravar: a segunda leitura daria outras contagens. Uma prévia nova do mesmo arquivo já importado mostra 0 novas. A importação gravada se desfaz só no banco, pelo roteiro abaixo; desfazer uma importação pela API é a #54.

**Parcelas.** Depois de importar uma fatura, as despesas parceladas só ganham a cobrança das parcelas pela conciliação da API, `POST usuarios/{idUsuario}/despesas/conciliacoes`. A conciliação automática é a #160; até lá, rode esse passo à mão.

### Reimportar do zero

Para recomeçar o banco local a partir dos arquivos de `dados/`:

1. Publique o servidor (`pwsh scripts/PublicarMcp.ps1`) e reconecte o cliente.
2. Limpe os dados importados com o seu script SQL local, fora do git. A limpeza não é uma ferramenta do servidor.
3. Importe pelo servidor os arquivos de `dados/`, extratos e faturas em ordem cronológica, cada um pela prévia e pela confirmação, resolvendo pela API o que a prévia pedir.
4. Concilie as parcelas pela API.
5. Rode o roteiro de conferência abaixo de ponta a ponta.

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
| "Quais cartões eu tenho?" | `listar_cartoes` | os cartões com banco, final e dias de fechamento e de vencimento |
| "A saída `<saída>` pagou a fatura do `<banco>` que vence em `<mês>`", e recuse o pedido de aprovação | `listar_cartoes`, `prever_pagamento_fatura` e o pedido da `confirmar_pagamento_fatura` | o resumo com o banco, o final, a quantidade e o total nos argumentos do pedido; recusado, as despesas continuam pendentes |
| A mesma pergunta, aceitando o pedido | `prever_pagamento_fatura` e `confirmar_pagamento_fatura` | o pedido aparece também no modo auto; aceito, as despesas cobradas da fatura ficam pagas pela saída, com o valor cobrado |
| "Importe o extrato `dados/<arquivo>` do C6", e recuse o pedido de aprovação | `prever_importacao` e o pedido da `confirmar_importacao` | o resumo com o layout, o final da conta (4 caracteres, nunca o número inteiro do cabeçalho), o banco, o período e as contagens nos argumentos do pedido, e nenhuma descrição de lançamento na conversa; recusado, nada é importado |
| A mesma pergunta, aceitando o pedido | `prever_importacao` e `confirmar_importacao` | aceito, as transações aparecem em `listar_transacoes`; uma nova prévia do mesmo arquivo mostra 0 novas |
| "Importe a fatura `dados/<arquivo>` do Mercado Pago" com um final sem cartão | `prever_importacao` | o erro com os finais sem cartão, e nada gravado |

`banco indisponivel` no primeiro passo indica o Postgres parado ou a conexão errada no User Secret; uma ferramenta de leitura que falha com o banco disponível indica o `Mcp:IdUsuario` ausente.
