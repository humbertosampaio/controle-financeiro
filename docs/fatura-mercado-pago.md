# Exportar a fatura do cartão do Mercado Pago

O aplicativo e o site do Mercado Pago não exportam a fatura do cartão de crédito. O caminho é pedir ao **Mago**, o assistente de IA do Mercado Pago, que monte o relatório em CSV.

O Mago responde no formato que o prompt pedir, então o importador só funciona se o pedido for **sempre o mesmo**. Use o prompt abaixo sem alterar nada além do mês de vencimento: mudar uma coluna, a ordem ou uma regra de formatação quebra a leitura do arquivo.

## Passo a passo

1. Abra o Mago no aplicativo do Mercado Pago.
2. Cole o prompt abaixo, trocando `MM/AAAA` pelo mês de **vencimento** da fatura (ex.: `09/2026`).
3. Copie só o bloco do CSV da resposta, do cabeçalho até a última linha, e salve em `dados/mercado-pago-cartao-MM-AAAA.csv` (UTF-8). A pasta `dados/` é ignorada pelo git; o arquivo nunca entra no repositório.
4. Leia a seção "Conferência" da resposta. Se o Mago disser que algum lançamento ou detalhe não pôde ser consultado, ou que os lançamentos não explicam o total, peça de novo antes de importar.

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

Observado na fatura de 09/2026, a primeira exportada assim:

- O cabeçalho e a ordem das 19 colunas são os do prompt; todas as células vêm entre aspas, mesmo as vazias.
- `tipo_lancamento` é um código em minúsculas com `_` (ex.: `compra_a_vista`, `compra_parcelada`, `pagamento_debito_automatico_pos_fechamento`). A lista não é fechada: outro mês pode trazer tipos novos, como estorno, tarifa ou juros.
- `status` é texto livre do Mago e não serve para regra.
- Nas compras, `data_lancamento` costuma vir vazia; a data que importa é `data_compra`. No pagamento da fatura é o contrário: vem só a `data_lancamento`, sem cartão, produto nem pedido.
- Cobrança e crédito ficam em colunas separadas, ambas positivas; o sinal sai do tipo de lançamento.
- `numero_transacao` vem em todas as linhas.

Como o arquivo é gerado por IA, o layout pode variar mesmo com o prompt fixo. Se a importação recusar um arquivo novo, confira primeiro se o cabeçalho e o formato batem com o prompt antes de mudar o leitor.
