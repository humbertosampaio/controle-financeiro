# Leitores de extrato e fatura

- **Um adaptador por layout**, da mesma porta de importação (inviolável 7 do `CLAUDE.md` da raiz). O parsing específico de um banco não vaza para o domínio nem para a Application.
- **O leitor documenta o que espera do arquivo.** O `<remarks>` de cada leitor lista o que se viu nos arquivos reais e de que a leitura depende: o sinal das entradas, o que cada categoria ou tipo significa, como se reconhece o pagamento da fatura. Ganha teste a expectativa que decide o que o leitor faz e que uma mudança no código poderia quebrar; o cenário raro e pouco sujeito a regressão pode ficar só documentado. Valor, descrição e nome de arquivo real nunca entram.
- **A importação é idempotente.** Reimportar o mesmo período não duplica transação. A chave de deduplicação é estável e documentada: o id do banco quando existe; senão, uma chave composta, com a posição entre linhas idênticas.
- Para conhecer um layout, leia em `dados/` só o cabeçalho e o formato das colunas, e descreva a estrutura, nunca valores.
