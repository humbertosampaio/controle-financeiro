# Testes dos leitores

O que cobrir num leitor de extrato ou fatura, com o arquivo fictício de `Fixtures/`:

- lê a fixture e produz as transações esperadas (data, valor com sinal, descrição);
- os formatos do banco: decimal com vírgula ou ponto, milhar com ponto, data `dd/mm/aaaa`, `dd-mm-aaaa` ou ISO, BOM, Latin-1 ou UTF-8, linhas de cabeçalho antes da tabela;
- reimportar o mesmo arquivo, ou um período sobreposto, não duplica nada; duas transações idênticas no mesmo dia continuam sendo duas;
- linha malformada vira erro com o número da linha, sem derrubar o arquivo inteiro;
- quando o arquivo traz saldo, a soma bate com ele;
- cada expectativa do `<remarks>` do leitor que decide o que ele faz (`src/ControleFinanceiro.Infrastructure.Importacao/CLAUDE.md`).

O `ArquivoFixture` lê o arquivo de `Fixtures/` e monta as variações dele no layout de bytes do banco.
