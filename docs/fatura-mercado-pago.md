# Exportar a fatura do cartão do Mercado Pago

O aplicativo e o site do Mercado Pago não exportam a fatura do cartão de crédito. O caminho é pedir ao **Mago**, o assistente de IA do Mercado Pago, que monte o relatório em CSV.

O Mago responde no formato que o prompt pedir, então o importador só funciona se o pedido for **sempre o mesmo**. Use o prompt abaixo sem alterar nada além do mês de vencimento: mudar uma coluna, a ordem ou uma regra de formatação quebra a leitura do arquivo.

## Passo a passo

1. Abra o Mago no aplicativo do Mercado Pago.
2. Cole o prompt abaixo, trocando `MM/AAAA` pelo mês de **vencimento** da fatura (ex.: `09/2026`).
3. Copie só o bloco do CSV da resposta, do cabeçalho até a última linha, e salve em `dados/mercado-pago-cartao-MM-AAAA.csv` (UTF-8), com `MM-AAAA` sendo o mês do **fechamento**, e não o do vencimento que vai no prompt: é o mês que o pagamento da fatura traz no nome ("Fatura de <mês>"). A pasta `dados/` é ignorada pelo git; o arquivo nunca entra no repositório.
4. Leia a seção "Conferência" da resposta. Se o Mago disser que algum lançamento ou detalhe não pôde ser consultado, ou que os lançamentos não explicam o total, peça de novo antes de importar.
5. Faça a prévia (`POST usuarios/{idUsuario}/importacoes/faturas/mercado-pago-cartao-csv/previa`) e resolva o que ela apontar: o final sem cartão se cadastra com o dia de vencimento e o de fechamento (`POST usuarios/{idUsuario}/cartoes`), porque a importação não cria cartão e recusa o arquivo enquanto faltar algum.
6. Importe pela rota da fatura (`POST usuarios/{idUsuario}/importacoes/faturas/mercado-pago-cartao-csv`).

## Prompt

```text
Prepare um relatório detalhado da minha fatura do cartão de crédito Mercado Pago com vencimento em MM/AAAA.

ESCOPO
- Localize a fatura pelo mês de vencimento, não pelo mês da compra ou do fechamento.
- Inclua todos os lançamentos: compras à vista, parcelas de compras antigas, assinaturas, tarifas, juros, impostos, estornos, créditos e pagamentos registrados nela.
- Consulte os detalhes das atividades vinculadas para identificar produtos, vendedor, pedido e referências, quando disponíveis. Não se limite à descrição genérica da fatura.
- Não substitua a fatura solicitada pela atual. Se houver ambiguidade, peça esclarecimento.

FORMATO FIXO DA RESPOSTA
1. Resumo: período de referência, fechamento, vencimento, situação, total da fatura e saldo restante, conforme disponíveis.
2. CSV em bloco de texto, separado por ponto e vírgula, sempre com este cabeçalho e nesta ordem:
mes_referencia;vencimento;fechamento;data_compra;data_lancamento;tipo_lancamento;descricao_fatura;vendedor;produtos;numero_pedido;numero_transacao;cartao_final;parcela_atual;parcelas_totais;moeda;valor_compra_original;valor_cobrado_fatura;valor_creditado_fatura;status
3. Conferência: informe se os lançamentos explicam o total da fatura e destaque qualquer diferença ou informação indisponível.

REGRAS DE CONSISTÊNCIA
- Uma linha por lançamento da fatura, sem duplicações. Se houver vários produtos no mesmo lançamento, reúna-os na mesma célula, separados por " | "; não repita o valor por produto.
- Para compras parceladas, registre o valor da parcela cobrada nesta fatura, separado do valor original da compra.
- Use valores positivos nas colunas de cobrança e crédito, distinguindo-os pelo tipo de lançamento. Não trate todo pagamento como dedução do total originalmente faturado: diferencie total da fatura e saldo restante.
- Use datas DD/MM/AAAA, referência MM/AAAA e valores sem símbolo monetário ou separador de milhares, com vírgula decimal. Identifique a moeda na coluna própria.
- Coloque todas as células entre aspas duplas e duplique aspas internas. Deixe campos indisponíveis vazios.
- Não invente nomes de vendedores, produtos, vínculos, datas ou valores. Marcas e nomes de produtos não comprovam quem é o vendedor.
- Ordene por data de lançamento; quando ausente, por data da compra, e depois por número da transação.
- Não declare o relatório completo se houver páginas, lançamentos ou detalhes que não puderam ser consultados. Explique as limitações fora do CSV.
- Entregue o CSV em texto; não afirme ter criado um arquivo para download sem confirmação dessa criação.
```

## O que esperar do arquivo

Observado nas faturas com vencimento em 09/2026 e 10/2026, as primeiras exportadas assim:

- O cabeçalho e a ordem das 19 colunas são os do prompt; todas as células vêm entre aspas, mesmo as vazias. O leitor recusa colunas a mais, a menos ou em outra ordem.
- O fim de linha varia: uma fatura veio com CRLF, e a outra com LF. O leitor aceita os dois.
- `mes_referencia` é o mês do vencimento, e não o do fechamento.
- `tipo_lancamento` é um código em minúsculas com `_` (ex.: `compra_a_vista`, `compra_parcelada`, `pagamento_debito_automatico_pos_fechamento`). A lista não é fechada: outro mês pode trazer tipos novos, como estorno, tarifa ou juros. O leitor só aceita os tipos que conhece e recusa o arquivo com o número da linha do tipo novo, que entra na lista do leitor, com teste, quando aparecer.
- `status` é texto livre do Mago e não serve para regra.
- Nas compras, `data_lancamento` costuma vir vazia; a data que importa é `data_compra`. No pagamento da fatura é o contrário: vem só a `data_lancamento`, sem cartão, produto nem pedido.
- Cobrança e crédito ficam em colunas separadas, ambas positivas; o sinal sai do tipo de lançamento.
- `descricao_fatura` também reconhece a compra parcelada entre faturas: uma descrição diferente na fatura seguinte gera outra despesa.
- `numero_transacao` vem em todas as linhas e não se repete dentro do arquivo, mas se repete entre faturas: a mesma compra parcelada traz o mesmo número em todas as parcelas. Por isso o leitor reconhece o lançamento pelo número, pela parcela e pelo valor.
- O pagamento da fatura vem sem `cartao_final`; o leitor o põe no menor final do arquivo.
- `vendedor` vem vazio, apesar de o prompt pedir.

Como o arquivo é gerado por IA, o layout pode variar mesmo com o prompt fixo. Se a importação recusar um arquivo novo, confira primeiro se o cabeçalho e o formato batem com o prompt antes de mudar o leitor.
