# Modelo de domínio

Retrato do `src/ControleFinanceiro.Domain`: agregados, entidades, value objects e como se referenciam. Mostra o que é estável (estrutura, relações, invariantes principais): as classes não listam propriedades, só os métodos que carregam regra de negócio. **Quem muda agregado, entidade, value object ou relação no Domain atualiza este arquivo no mesmo PR**; o `revisor-dominio` cobra.

Legenda:

- `<<AggregateRoot>>`: raiz de agregado, carregada e gravada inteira pelo seu repositório.
- `<<Entity>>`: entidade interna de um agregado, só alcançada pela raiz.
- `<<ValueObject>>` e `<<SmartEnum>>`: sem identidade, comparados pelo valor.
- `*--` composição (faz parte do agregado); `..>` referência por id a outro agregado; `--|>` herança.
- Ids são structs Vogen sobre `Guid` (`IdUsuario`, `IdDespesa`…), gerados com `FromNewVersion7Guid`.
- Todo agregado com `..> Usuario : IdUsuario` implementa `IPertenceAoUsuario` e é filtrado pelo usuário atual.

Entre agregados, a referência é sempre pelo id da raiz: nenhum agregado guarda outro nem entidade de outro.

## Agregados

```mermaid
classDiagram
    direction LR

    namespace Usuarios {
        class Usuario {
            <<AggregateRoot>>
        }
        class DescontoSalarial {
            <<Entity>>
        }
    }

    namespace Cartoes {
        class Cartao {
            <<AggregateRoot>>
            AlterarCicloFatura(DiaVencimentoCartao, DiaFechamentoCartao)
            PossuiBancoESufixo(Banco, SufixoCartao)
            EhDaMesmaFaturaQue(Cartao)
            MesVencimento(dataCompra)
            VencimentoDaCompra(dataCompra)
            VencimentoNoMes(MesReferencia)
            DataOcorrenciaQueVenceEm(MesReferencia, DiaValidoEmTodoMes)
        }
        class VencimentoFatura {
            <<ValueObject>>
            Mes
            Dia
            Seguinte()
            DateOnly (implícito)
        }
        class FaturaCartao {
            <<ValueObject>>
            Cartoes
            Vencimento
            DoCartao(Cartao, cartoesDoUsuario, MesReferencia)
            DoCartao(IdCartao, cartoesDoUsuario, MesReferencia)
            DaCompra(IdCartao, dataCompra, cartoesDoUsuario)
            Contem(IdCartao)
        }
    }

    namespace Contas {
        class ContaBancaria {
            <<AggregateRoot>>
            PossuiBancoESufixo(Banco, SufixoConta)
        }
    }

    namespace Despesas {
        class DespesaRecorrente {
            <<AggregateRoot>>
            PossuiBeneficiarioEDescricao(IdBeneficiario, TextoObrigatorio)
        }
        class Despesa {
            <<AggregateRoot, abstract>>
            AlterarCategoria(IdCategoriaDespesa)
            PagamentoDoMes(MesReferencia)
            ValorVigenteDoMes(MesReferencia)
            PagamentoCobradoPor(IdTransacao)
            PagamentosRealizadosPor(IdTransacao)
        }
        class DespesaInteira {
            <<abstract>>
            MesReferencia
            ValorVigente
        }
        class DespesaSimples {
            PagarCom(IdTransacao, dataEfetiva, valorEfetivo)
            DesfazerPagamento(IdTransacao)
        }
        class IDespesaDoCartao {
            <<interface>>
            IdCartao
            FoiPagaSoPelaFatura(FaturaCartao, IdTransacao)
            PagarFatura(FaturaCartao, IdTransacao, dataPagamento)
            DesfazerPagamento(IdTransacao)
        }
        class DespesaAdiada {
            CriarCobrada(IdUsuario, IdBeneficiario, descricao, IdCategoria, dataOcorrencia, IdCartao, VencimentoFatura, Cobranca)
            EhPrevisaoDe(FaturaCartao, IdBeneficiario)
            CobrarCom(Cobranca)
            DesfazerCobranca(IdTransacao)
        }
        class DespesaParcelada {
            Criar(IdUsuario, IdBeneficiario, descricao, IdCategoria, dataOcorrencia, valor, IdCartao, vencimentoPrimeiraParcela, Parcela)
            ParcelaDoMes(MesReferencia)
            ParcelaCobradaPor(IdTransacao)
            ParcelasPagasPor(IdTransacao)
            EhDaCompra(ChaveCompraParcelada)
            ParcelaNumero(numero)
            CobrarParcela(Parcela, Cobranca)
            DesfazerCobranca(IdTransacao)
        }
        class ParcelaDespesa {
            <<Entity>>
            ValorProjetado
            ValorVigente
            MesReferencia
            TemCobranca
            CobrarCom(Cobranca)
            PagarFatura(IdTransacao, dataPagamento)
            DesfazerPagamento()
            DesfazerCobranca()
        }
        class Pagamento {
            <<ValueObject>>
            Pendente(valorProjetado, dataProjetada)
            Pago(valorProjetado, dataProjetada, dataEfetiva)
            PagarCom(IdTransacao, dataEfetiva, valorEfetivo)
            PagarPelaFatura(Cobranca, IdTransacao, dataPagamento)
            Reabrir()
            Reabrir(IdTransacao)
            ValorVigenteCom(Cobranca)
        }
        class Cobranca {
            <<ValueObject>>
        }
        class ChaveCompraParcelada {
            <<ValueObject>>
            DaTransacao(Transacao)
            DoLancamento(IdCartao, Lancamento)
        }
        class ParcelaDaCompra {
            <<ValueObject>>
            DaTransacao(Transacao)
        }
    }

    namespace Beneficiarios {
        class Beneficiario {
            <<AggregateRoot>>
            RemoverAlias(Alias)
            Renomear(TextoObrigatorio)
            PossuiAlias(Alias)
        }
        class AliasBeneficiario {
            <<ValueObject>>
        }
    }

    namespace Shared {
        class Alias {
            <<ValueObject>>
        }
    }

    namespace Categorias {
        class CategoriaDespesa {
            <<AggregateRoot>>
            RemoverAlias(Alias)
            PossuiAlias(Alias)
            PossuiNome(TextoObrigatorio)
            EhOutros
        }
        class AliasCategoria {
            <<ValueObject>>
        }
    }

    namespace Transacoes {
        class Lancamento {
            <<ValueObject>>
        }
        class Transacao {
            <<AggregateRoot>>
            EhOrigemConta
            AliasTextoBeneficiario
            AliasCategoriaBanco
            EhCompraAVistaNoCartao
        }
        class OrigemTransacao {
            <<ValueObject>>
        }
        class Parcela {
            <<ValueObject>>
        }
    }

    namespace Importacoes {
        class ImportacaoArquivo {
            <<AggregateRoot>>
            PeriodoNaoImportado()
        }
    }

    Usuario "1" *-- "*" DescontoSalarial
    Cartao ..> Usuario : IdUsuario
    ContaBancaria ..> Usuario : IdUsuario

    DespesaRecorrente ..> Usuario : IdUsuario
    DespesaRecorrente ..> Cartao : IdCartao (só no crédito)
    DespesaRecorrente ..> Beneficiario : IdBeneficiario
    DespesaRecorrente ..> CategoriaDespesa : IdCategoria

    Despesa <|-- DespesaInteira
    Despesa <|-- DespesaParcelada
    DespesaInteira <|-- DespesaSimples
    DespesaInteira <|-- DespesaAdiada
    IDespesaDoCartao <|.. DespesaAdiada
    IDespesaDoCartao <|.. DespesaParcelada
    IDespesaDoCartao ..> FaturaCartao : PagarFatura
    FaturaCartao ..> Cartao : IdCartao (os da mesma fatura)
    FaturaCartao *-- VencimentoFatura
    DespesaParcelada "1" *-- "*" ParcelaDespesa
    DespesaInteira *-- Pagamento
    ParcelaDespesa *-- Pagamento
    DespesaAdiada *-- "0..1" Cobranca
    ParcelaDespesa *-- "0..1" Cobranca
    Despesa ..> Usuario : IdUsuario
    Despesa ..> Beneficiario : IdBeneficiario
    Despesa ..> CategoriaDespesa : IdCategoria (opcional)
    Despesa ..> DespesaRecorrente : IdDespesaRecorrente (quando gerada por uma)
    DespesaAdiada ..> Cartao : IdCartao
    DespesaParcelada ..> Cartao : IdCartao
    DespesaParcelada ..> ChaveCompraParcelada : EhDaCompra
    ChaveCompraParcelada ..> Cartao : IdCartao
    ParcelaDaCompra *-- ChaveCompraParcelada
    ParcelaDaCompra ..> Transacao : DaTransacao
    ChaveCompraParcelada ..> Transacao : DaTransacao
    ChaveCompraParcelada ..> Lancamento : DoLancamento
    Despesa ..> Transacao : IdTransacao (no Pagamento, a saída que pagou: o débito na simples, o pagamento da fatura na do cartão)
    Cobranca ..> Transacao : IdTransacao (a compra do cartão)

    Beneficiario "1" *-- "*" AliasBeneficiario
    AliasBeneficiario *-- Alias
    AliasBeneficiario ..> Usuario : IdUsuario
    Beneficiario ..> Usuario : IdUsuario
    CategoriaDespesa "1" *-- "*" AliasCategoria
    AliasCategoria *-- Alias
    AliasCategoria ..> Usuario : IdUsuario
    CategoriaDespesa ..> Usuario : IdUsuario

    Transacao *-- OrigemTransacao
    Transacao *-- "0..1" Parcela
    Transacao ..> Usuario : IdUsuario
    Transacao ..> ImportacaoArquivo : IdImportacao
    OrigemTransacao ..> ContaBancaria : IdConta
    OrigemTransacao ..> Cartao : IdCartao
    ImportacaoArquivo *-- OrigemTransacao
    ImportacaoArquivo ..> Usuario : IdUsuario
```

### Invariantes que valem saber

- **Usuario**: `RendaLiquida` = renda bruta − descontos.
- **Cartao**: um por banco + sufixo em cada usuário. Só nasce pela `CartaoFactory`, que recebe os cartões do usuário: o `Criar` falha com `ErroCartaoJaCadastrado`, e o `ObterDosFinais` devolve o cartão de cada final de uma fatura, ou falha com `ErroCartoesNaoResolvidos`: a importação nunca cria cartão. O índice único da tabela é a rede para duas requisições ao mesmo tempo. O dia de vencimento e o de fechamento são obrigatórios, sem regra entre eles. Os dois podem ser alterados depois, e os dias novos valem só para as despesas geradas a partir daí: as já geradas e as parcelas já criadas guardam a data projetada do pagamento. O `MesVencimento` calcula, pelos dias do cartão, o mês de vencimento da fatura em que cai uma compra, e o `DataOcorrenciaQueVenceEm` faz a volta: a data da compra que vence num mês pedido. O `VencimentoFatura` junta o mês e o dia de vencimento, que é o mesmo para todos os finais da mesma fatura, e vira a data pela conversão implícita para `DateOnly`; o `VencimentoDaCompra` e o `VencimentoNoMes` o montam pelo cartão, e a `DespesaAdiada`, a `DespesaParcelada` e a `FaturaCartao` o recebem no lugar do par solto. A compra no dia do fechamento entra na fatura que fecha nesse dia.
- **ContaBancaria**: uma por banco + sufixo em cada usuário, com o apelido padrão "&lt;tipo&gt; &lt;banco&gt; final &lt;sufixo&gt;". Só nasce pela `ContaBancariaFactory.ObterOuCriar`, que devolve a conta existente ou uma nova, e falha com `ErroContaDeOutroTipo` se a conta existe com outro tipo.
- **Beneficiario**: quem recebeu o dinheiro de uma saída, reconhecido no arquivo só pelos aliases, nunca pelo nome. Pode nascer sem alias (cadastro manual, sem arquivo). Cada alias aponta para um beneficiário só em cada usuário: o beneficiário só nasce e só ganha alias pela `BeneficiarioFactory`, que recebe os beneficiários do usuário: o `Resolver` aplica um lote inteiro ou nada (`BeneficiarioNovo`, com ou sem alias, e `AliasBeneficiarioExistente`) e falha com `ErroAliasBeneficiarioJaAtribuido`. O nome não é único: o usuário o escolhe livremente. O índice único `(id_usuario, texto_normalizado)` é a rede, e por isso o `AliasBeneficiario` guarda o `IdUsuario`. Renomear e remover alias não passam pela factory.
- **CategoriaDespesa**: categoria de despesa do usuário. Os aliases são as categorias do banco que ele associou a ela; a busca casa só com eles, nunca com o nome. O nome é único em cada usuário, comparado sem caixa, acentos e espaços repetidos, e cada alias aponta para uma categoria só. A categoria só nasce, só ganha alias e só é renomeada pela `CategoriaDespesaFactory`, que recebe as categorias do usuário: o `Resolver` aplica um lote inteiro ou nada (categorias novas, com ou sem alias, e aliases para as existentes) e falha com `ErroCategoriaJaCadastrada` ou `ErroAliasCategoriaJaAtribuido`. O índice único `(id_usuario, texto_normalizado)` dos aliases é a rede. Todo usuário tem a categoria `Outros` (`CategoriaDespesa.NomeOutros`), criada junto com ele pela `CriarOutros`: é achada pelo nome, então não pode ser renomeada (`ErroCategoriaOutrosNaoRenomeavel`). Remover alias não passa pela factory.
- **DespesaRecorrente**: beneficiário e categoria obrigatórios; cartão obrigatório no crédito e proibido nas outras formas. Só nasce pela `DespesaRecorrenteFactory`, com uma entrada por forma de pagamento (`CriarNoCredito`, com o `Cartao`, e `CriarSemCartao`), que recebe o beneficiário, a categoria e o cartão do usuário e as recorrentes dele: falha com `ErroDespesaRecorrenteJaCadastrada` quando ele já tem uma do mesmo beneficiário com a mesma descrição, sem contar caixa, acentos e espaços repetidos.
- **Despesas**: datas sem hora nem fuso (`DateOnly`). Beneficiário obrigatório e categoria opcional (a fatura do Mercado Pago não traz categoria). A gerada por uma recorrente guarda o `IdDespesaRecorrente`. `DespesaSimples` não aceita crédito: o `Criar` a registra paga no dia em que ocorre, e o `Prever` a gera pendente, com o pagamento projetado para esse dia; `DespesaAdiada` é sempre crédito, guarda o `IdCartao` e tem o pagamento pendente no mês de vencimento da fatura em que a ocorrência cai, no dia de vencimento do cartão; `DespesaParcelada` é sempre crédito, só nasce pela `DespesaParceladaFactory`, guarda o `IdCartao` e tem uma `ParcelaDespesa` por mês consecutivo, de 1 ao total, todas com o mesmo valor: as anteriores à parcela informada nascem pagas, com o vencimento projetado como data efetiva, e as demais, pendentes. A `ParcelaDespesa` só é criada pela raiz. A categoria de qualquer despesa pode ser trocada depois, mas não removida. O mês de referência mora no `Pagamento`, e a `DespesaInteira` e a `ParcelaDespesa` o leem dele (`MesReferencia`). O `PagamentoDoMes` dá o pagamento que cai num mês: o da `DespesaInteira` quando o mês é o dela, o da parcela daquele mês na `DespesaParcelada`, ou nada. Duas buscas fazem o caminho inverso a partir da transação: o `PagamentoCobradoPor` dá o pagamento da despesa do cartão ou da parcela que a compra cobrou, ou nada (a simples não é cobrada), e o `PagamentosRealizadosPor`, os pagamentos que a transação pagou; na parcelada, o `ParcelaCobradaPor` e o `ParcelasPagasPor` dão as próprias parcelas. O `ValorVigenteDoMes` dá o valor que vale para o pagamento do mês: o efetivo quando pago, o cobrado quando a fatura o cobrou, senão o projetado.
- **Cobrança e pagamento**: só a despesa muda, e a `Transacao` é apenas lida; a cobrança e o pagamento da simples só nascem e só se desfazem pelo `VinculoTransacaoService`, e o pagamento da despesa do cartão nasce pelo `PagamentoFaturaService`. A `DespesaSimples` é paga pela própria saída da conta: o `Pagamento` guarda a transação (`IdTransacao`), a data e o valor efetivos. Na despesa do cartão há dois fatos: a **cobrança**, a compra lançada na fatura, que liga a transação do cartão à `DespesaAdiada` ou à `ParcelaDespesa` e fixa o valor real (`Cobranca`), e o **pagamento**, a saída da conta que pagou a fatura. Cobrar não paga: o `Pagamento` continua como está, pendente, ou pago, na parcela que nasceu paga. Sem status novo: "cobrada e não paga" é derivado, e o valor vigente é o cobrado. Uma transação cobra no máximo uma despesa ou parcela, e cada uma é cobrada por uma compra só (`ErroDespesaJaCobrada`); uma saída paga no máximo uma simples, enquanto um pagamento de fatura paga várias despesas do cartão, e só elas: a saída que paga uma não paga a outra. Valor diferente do projetado é aceito, sem tolerância e sem mudar as parcelas seguintes. O `Pago` sem transação tem o efetivo igual ao projetado, e a simples que nasceu paga sem transação aceita a saída. Desfazer é pela origem da transação: o `DesfazerPagamento` volta a simples para pendente, com os efetivos nulos, e o `DesfazerCobranca` tira a cobrança; a despesa do cartão paga por transação falha com `ErroDespesaJaPaga`, e a parcela que nasceu paga perde a cobrança e continua paga. A transação ligada não pode ser apagada: as FKs impedem, conferidas só no commit para a cascata do usuário funcionar. O `ConciliacaoParcelasService` cobra as parcelas de uma vez, sempre pela regra da parcelada do `VinculoTransacaoService`, e a idempotência vem do estado: só entram transação livre e parcela sem cobrança. Limitação aceita: conciliar de novo refaz uma cobrança desfeita, porque a recusa não é registrada (#123). Quem desfaz para trocar gêmeas liga manualmente em seguida. O domínio fala em cobrança e pagamento; "vínculo" fica na fronteira (rotas, `prever_vinculo`, `confirmar_vinculo`, `ErroTransacaoJaVinculada`).
- **Pagamento da fatura**: o `PagamentoFaturaService` paga a fatura com uma saída da conta, de qualquer conta do usuário, informada com um cartão e o mês de vencimento; a entrada no cartão nunca paga. A fatura (`FaturaCartao`) junta os cartões que saem nela (`Cartao.EhDaMesmaFaturaQue`: mesmo banco, dia de vencimento e dia de fechamento), como os finais do C6, e o mês de vencimento. Só a despesa do cartão (`IDespesaDoCartao`: a adiada e a parcelada) é paga assim: o `PagarFatura`, que só o serviço alcança, paga o pagamento que vence no mês da fatura quando a despesa é de um cartão dela e o pagamento tem cobrança e está pendente (`Pagamento.PagarPelaFatura`), com a data da saída e o valor cobrado como efetivo, e deixa o resto como está, inclusive a parcela que nasceu paga. Cada despesa muda sozinha. Repetir com a mesma saída e a mesma fatura paga as cobranças que chegaram depois; a saída que já pagou outro mês ou outra fatura (`FoiPagaSoPelaFatura` falso) falha com `ErroTransacaoPagaOutraFatura`, e, sem nada a pagar, com `ErroFaturaSemCobrancaPendente`, que costuma ser o mês ou o cartão errado. Limitações aceitas: pagamento parcial e rotativo ficam fora, e valor e data da saída não bloqueiam, porque o acerto da ligação é de quem liga. O `Desfazer` desfaz o pagamento pela saída: as despesas do cartão e as parcelas que ela pagou voltam a pendente pelo `DesfazerPagamento` (`Pagamento.Reabrir(IdTransacao)`), com a cobrança preservada, e cada uma muda sozinha. A despesa do cartão que a saída não pagou falha com `ErroTransacaoNaoPagouADespesa`, que vem da própria despesa; o serviço só cria esse erro quando a saída não pagou nenhuma despesa do cartão. A simples paga pela mesma saída fica como está, porque se desfaz pelo `VinculoTransacaoService`.
- **ChaveCompraParcelada**: reconhece a mesma compra parcelada entre faturas, por cartão, data, descrição normalizada e total de parcelas. Recebe a descrição já como `TextoNormalizado`. A `DespesaParcelada` não a guarda: o `EhDaCompra` a monta para comparar.
- **Transacao**: valor com sinal do ponto de vista do usuário (negativo é saída). Só nasce pela `LoteTransacoesFactory`, que confere que a conta ou o cartão é do usuário, que a importação é da mesma origem, que os lançamentos são do mesmo tipo de origem, que todo texto de beneficiário do lote já é alias de um beneficiário do usuário e que toda categoria do banco já é alias de uma categoria dele, e garante a `ChaveDeduplicacao` do lote. A identidade é o `IdTransacao`; a chave só reconhece o lançamento numa reimportação. Guarda a importação que a criou: a reimportação não a recria, então ela fica ligada à primeira importação que a trouxe. O `TextoBeneficiario`, calculado e sem coluna, é o texto do arquivo que diz quem recebeu o dinheiro de uma saída, pela mesma regra do `Lancamento`. O beneficiário é o que tem esse texto como alias, resolvido na leitura: a transação não guarda o id dele.
- **OrigemTransacao**: conta ou cartão, nunca os dois; o `Tipo` (`TipoOrigem`) diz qual.
- **Parcela**: a parcela que o banco informou (`3/10`), com total a partir de 2 e número de 1 ao total. A compra à vista, e a `1/1`, não têm parcela; a `Transacao` diz se é parcelada pelo `EhParcelada`. Só a fatura traz parcela, então a transação com parcela é sempre de um cartão, e o `CK_transacoes_parcela` repete isso no banco. Não é a `ParcelaDespesa`.
- **ImportacaoArquivo**: só nasce pela `ImportacaoArquivoFactory`, que tira o usuário e a origem da própria conta ou do próprio cartão, antes do lote de transações. Quantas eram novas não fica nela: são as transações ligadas a ela. `PeriodoNaoImportado` aponta o buraco entre a importação anterior da mesma origem e esta.

### Erros

Cada falha prevista que é uma situação de negócio com nome próprio é um tipo derivado do `Erro` abstrato, na pasta do seu agregado, com igualdade só pelo `Codigo`; quem chama decide pelo tipo, e o endpoint mapeia cada tipo para um status. Valor que não pertence ao conceito, como a validação de um value object, falha com mensagem em texto (`Result<T>`).

| Erro | Agregado | Quando |
|---|---|---|
| `ErroCartaoJaCadastrado` | `Cartoes` | O usuário já tem um cartão do banco com o mesmo final. |
| `ErroCartoesNaoResolvidos` | `Cartoes` | A fatura a importar tem um final sem cartão do usuário no banco dela. Não traz os finais: a tela refaz a prévia. |
| `ErroAliasBeneficiarioJaAtribuido` | `Beneficiarios` | O alias já é de outro beneficiário do usuário; traz o id desse beneficiário. |
| `ErroBeneficiariosNaoResolvidos` | `Beneficiarios` | O arquivo a importar tem uma saída cujo texto de beneficiário ainda não é alias de nenhum beneficiário do usuário. Não traz os textos: a tela refaz a prévia. |
| `ErroCategoriasBancoNaoResolvidas` | `Categorias` | O arquivo a importar tem um lançamento cuja categoria do banco ainda não é alias de nenhuma categoria do usuário. Não traz os textos: a tela refaz a prévia. |
| `ErroContaDeOutroTipo` | `Contas` | O usuário já tem a conta, mas de outro tipo. |
| `ErroOrigemNaoPertenceAoUsuario` | `Transacoes` | A conta ou o cartão do lote é de outro usuário. |
| `ErroIdentificadorRepetido` | `Transacoes` | Dois lançamentos do lote têm o mesmo id do banco. |
| `ErroTransacaoJaVinculada` | `Despesas` | A transação já paga ou cobra uma despesa, que pode ser a mesma do pedido; traz o id dela. |
| `ErroDespesaJaPaga` | `Despesas` | A simples já foi paga por outra saída, ou, ao desfazer a cobrança, a despesa do cartão já foi paga por transação. Desfaz-se o pagamento antes. |
| `ErroDespesaJaCobrada` | `Despesas` | A despesa do cartão ou a parcela já foi cobrada por outra compra. Para trocar, desfaz-se a cobrança antes. |
| `ErroTransacaoNaoEhSaida` | `Despesas` | A transação é uma entrada ou tem valor zero. |
| `ErroOrigemIncompativelComDespesa` | `Despesas` | Simples paga com cartão; adiada cobrada pela conta ou por outro cartão; parcelada cobrada por outro cartão. |
| `ErroTransacaoPagaOutraFatura` | `Despesas` | No pagamento da fatura: a saída já pagou despesas de um cartão de outra fatura ou de outro mês. Desfaz-se esse pagamento antes. |
| `ErroFaturaSemCobrancaPendente` | `Despesas` | No pagamento da fatura: nenhuma despesa dos cartões da fatura, no mês, tem cobrança e está pendente. |
| `ErroParcelaIncompativelComDespesa` | `Despesas` | Transação com parcela numa despesa inteira, sem parcela numa parcelada, ou parcela de outra compra. |
| `ErroTransacaoNaoCobrouADespesa` | `Despesas` | No desfazer: a compra do cartão não cobrou a despesa nem as parcelas dela. |
| `ErroTransacaoNaoPagouADespesa` | `Despesas` | No desfazer do vínculo ou do pagamento da fatura: a saída da conta não pagou a despesa. |

## Importação de um extrato ou de uma fatura

O extrato, a fatura e os seus lançamentos são value objects: não têm identidade nem são gravados, mas são linguagem do negócio.

- **Extrato:** o adaptador de cada layout produz o `Extrato`, a `ImportacaoArquivoFactory` registra a importação, a `LoteTransacoesFactory` transforma os lançamentos em transações ligadas a ela e a `ConferenciaSomaPeriodo`, antes da gravação, compara a soma do período com o que já estava gravado mais as novas.
  - Quando o arquivo não traz a conta (`ExtratosContaInformada`, como o do Mercado Pago), o usuário informa o sufixo, e o adaptador o recebe.
  - Quando o arquivo traz o saldo inicial, ele vira o saldo da véspera do primeiro dia: o primeiro dia também é conferido, e o saldo final tem de ser o do último dia.
  - O período de um arquivo que não o declara vai da primeira à última data dos lançamentos.
- **Fatura:** o adaptador produz a `Fatura`, a `CartaoFactory.ObterDosFinais` devolve um cartão por final, e cada cartão ganha a sua importação e o seu lote, todos gravados juntos. A fatura não tem saldo, então não há conferência de soma nem aviso de período não importado.
  - Os layouts da lista `Faturas` entram pela mesma rota, escolhidos pelo campo `layout`.
  - A fatura do Mercado Pago identifica cada lançamento pelo número da transação, com a parcela e o valor (a identidade composta pelo identificador do banco), porque a parcela seguinte da mesma compra traz o mesmo número. O pagamento da fatura vem sem final e vai para o menor final do arquivo.
  - Limitação aceita no C6: duas compras à vista idênticas no dia do fechamento, uma em cada fatura, ganham a mesma chave, e a da segunda fatura é descartada.
  - Cada saída com parcela que ainda não tem despesa vira uma `DespesaParcelada` do cartão, pela `DespesaParceladaFactory`, gravada no mesmo commit das transações. O beneficiário e a categoria são os que têm os textos do lançamento como alias; sem categoria do banco, a despesa fica sem categoria. A primeira parcela fica no mês de vencimento da fatura em que a data da compra cai.
  - A mesma compra é reconhecida pela `ChaveCompraParcelada`: cartão, data, descrição normalizada e total de parcelas. O valor fica de fora, e uma descrição alterada pelo banco entre faturas gera outra despesa. Compras gêmeas (mesma chave e mesmo número na mesma fatura) viram uma despesa cada; compras distintas com a mesma chave em faturas diferentes viram uma só. No empate entre números da mesma chave, vale o menor.
  - A importação só cria: reimportar ou importar a fatura seguinte não altera a despesa existente. A ligação das parcelas com as transações é posterior, numa operação à parte (`ConciliacaoParcelasService`). Não há índice único nessa chave, então duas importações ao mesmo tempo podem criar a mesma despesa duas vezes.
  - Cada compra à vista nova (`Transacao.EhCompraAVistaNoCartao`: saída sem parcela da fatura, com alias do beneficiário) sem despesa prevista vira uma `DespesaAdiada` já cobrada por ela (`CriarCobrada`) e pendente, pela `DespesaAdiadaFactory`, gravada no mesmo commit, com o beneficiário e a categoria pelos aliases, como na parcelada. O pagamento da fatura e o estorno chegam como entrada e ficam de fora. A previsão (`EhPrevisaoDe`) é a adiada sem cobrança do mesmo beneficiário, no mês de vencimento da compra e num cartão da mesma fatura, para a recorrente prevista num final e cobrada noutro; cada uma cobre uma compra só, e a factory recebe as novas de todos os cartões do arquivo de uma vez para isso valer entre os finais. A importação não cobra a prevista: quem a liga à compra é o vínculo. Só as transações novas entram, então reimportar não cria de novo, e as compras à vista importadas antes desta regra não ganham despesa. Limitações aceitas: a adiada criada por engano (fechamento deslocado, alias diferente da previsão) não pode ser excluída, e uma previsão já usada por uma compra numa importação volta a valer na importação seguinte enquanto não for cobrada.
- **Cartões resolvidos:** a fatura só é importada quando todo final já tem cartão do usuário no banco dela: a `CartaoFactory.ObterDosFinais` falha com `ErroCartoesNaoResolvidos`, e nada é gravado. A prévia marca os finais sem cartão (`Cadastrado: false`), e o usuário os cadastra antes de importar. Esta conferência vem antes de todas as outras, porque sem os cartões não há lote.
- **Beneficiários resolvidos:** o extrato e a fatura só são importados quando todo texto de beneficiário das saídas já é alias de um beneficiário do usuário. A `LoteTransacoesFactory` recebe os beneficiários do usuário e, com o `AliasesBeneficiarioPendentes.Conferir`, aplica a mesma regra da prévia aos lançamentos do lote: falha com `ErroBeneficiariosNaoResolvidos`, e nada é gravado. Na fatura, cada cartão confere o seu lote, e o primeiro que falha interrompe a importação. A conferência vem depois da do id do banco repetido, para os problemas do próprio arquivo aparecerem primeiro.
- **Categorias do banco resolvidas:** pela mesma regra, o arquivo só é importado quando toda categoria do banco, de qualquer lançamento (inclusive a entrada e o estorno, que abate o gasto da categoria), já é alias de uma categoria do usuário. A `LoteTransacoesFactory` recebe as categorias do usuário e confere com o `AliasesCategoriaPendentes.Conferir`, que falha com `ErroCategoriasBancoNaoResolvidas`. As conferências do lote seguem a ordem: id do banco repetido, beneficiários, categorias; com beneficiário e categoria pendentes, sai o erro de beneficiário, e a prévia da fatura lista as duas pendências. Nenhum leitor de extrato preenche a categoria do banco hoje, então a prévia do extrato ainda não a lista; ela ganha a lista junto com o primeiro layout de extrato que a trouxer. Para deixar uma categoria do banco de lado, o usuário a associa à Outros. A transação não guarda o id da categoria: ela é resolvida na leitura.

- **Extrato**: só nasce com os saldos fechando. O `Criar` falha quando um dia contábil traz mais de um saldo, quando falta o saldo de um dia ou quando os lançamentos de um dia não levam o saldo do dia anterior ao saldo do dia. Agrupa pela data contábil; o primeiro dia só serve de base, e os dias contábeis depois do fim do período ficam de fora. As mensagens citam só as datas. O adaptador que conhece o saldo de abertura o passa como o saldo da véspera, e aí o primeiro dia com lançamento também é conferido.
- **IdentidadeLancamento**: única, pelo identificador do banco que basta sozinho (o `REFERENCE_ID` do extrato do Mercado Pago), ou composta, campo a campo. A composta começa pela data, quando o banco não dá identificador, ou por um identificador do banco que não basta sozinho, como o número que se repete nas parcelas da mesma compra. Só a única não pode se repetir no lote; as compostas iguais se distinguem pelo ordinal da `ChaveDeduplicacao`.
- **Lancamento**: a linha de extrato (`DaConta`) ou de fatura (`DoCartao`) que vira transação, com a parcela e a categoria do banco quando o layout as traz; a categoria e o `TipoOrigem` ficam fora da identidade. O `TextoBeneficiario` diz quem recebeu o dinheiro de uma saída: a contraparte na conta e a descrição no cartão; a entrada (inclusive o estorno e o pagamento da fatura) não tem. A prévia da importação o lê antes de existir transação. O `AliasTextoBeneficiario` e o `AliasCategoriaBanco` são nulos quando o texto falta ou fica vazio depois de normalizado. O `EhCompraParcelada` é a saída com parcela, com alias do beneficiário. Só a fatura traz parcela: o `DaConta` não a recebe. Duas linhas com os mesmos campos são iguais como valor; quem as distingue é a posição no lote, na `ChaveDeduplicacao`.
- **LancamentoContabilizado**: o lançamento com o dia contábil em que entrou no saldo. A data contábil só serve à conferência de saldo do `Extrato` e não é gravada.
- **Fatura**: o banco e os lançamentos na ordem do arquivo, com pelo menos um. Separa os lançamentos por final (`Sufixos`, `LancamentosCartao`) e dá o período de cada cartão, da menor à maior data de compra (`PeriodoCartao`). Um final fora da fatura é bug de quem chama.
- **LancamentoFatura**: o lançamento com o final do cartão que fez a compra. O final só separa os cartões e não é gravado: a transação guarda o id do cartão.

```mermaid
classDiagram
    direction LR

    class Extrato {
        <<ValueObject>>
        Criar()
    }
    class Fatura {
        <<ValueObject>>
        Criar()
        LancamentosCartao(sufixo)
        PeriodoCartao(sufixo)
    }
    class LancamentoFatura {
        <<ValueObject>>
    }
    class Parcela {
        <<ValueObject>>
    }
    class CartaoFactory {
        <<service>>
        ObterDosFinais(IdUsuario, cartoesDoUsuario, banco, sufixos)
    }
    class Cartao {
        <<AggregateRoot>>
    }
    class LancamentoContabilizado {
        <<ValueObject>>
    }
    class Lancamento {
        <<ValueObject>>
        DaConta()
        DoCartao()
        TipoOrigem
        ValorAbsoluto
        TextoBeneficiario
        AliasTextoBeneficiario
        AliasCategoriaBanco
        EhCompraParcelada
    }
    class IdentidadeLancamento {
        <<ValueObject>>
        Unica()
        Composta()
    }
    class SaldoDiario {
        <<ValueObject>>
    }
    class PeriodoArquivo {
        <<ValueObject>>
    }
    class ChaveDeduplicacao {
        <<ValueObject>>
        CalcularLote()
    }
    class ConferenciaSomaPeriodo {
        <<service>>
        Conferir(Extrato, gravadasNoPeriodo, novas)
    }
    class ImportacaoArquivoFactory {
        <<service>>
        Registrar(ContaBancaria ou Cartao, layout, periodo, lidas, importadaEm)
    }
    class LoteTransacoesFactory {
        <<service>>
        Criar(IdUsuario, ContaBancaria ou Cartao, ImportacaoArquivo, lancamentos)
    }
    class DespesaParceladaFactory {
        <<service>>
        CriarDaFatura(IdUsuario, beneficiarios, categorias, Cartao, parceladas, lancamentos)
    }
    class DespesaAdiadaFactory {
        <<service>>
        CriarDaFatura(IdUsuario, beneficiarios, categorias, cartoes, adiadas, transacoesNovas)
    }
    class DespesaParcelada {
        <<AggregateRoot>>
    }
    class DespesaAdiada {
        <<AggregateRoot>>
    }
    class ImportacaoArquivo {
        <<AggregateRoot>>
    }
    class Transacao {
        <<AggregateRoot>>
    }

    Extrato "1" *-- "*" LancamentoContabilizado
    Extrato "1" *-- "*" SaldoDiario
    Extrato *-- PeriodoArquivo
    LancamentoContabilizado *-- Lancamento
    Lancamento *-- IdentidadeLancamento
    Lancamento *-- "0..1" Parcela
    Fatura "1" *-- "*" LancamentoFatura
    LancamentoFatura *-- Lancamento
    CartaoFactory ..> Fatura : um cartão por final
    CartaoFactory ..> Cartao : obtém
    ConferenciaSomaPeriodo ..> Extrato : soma os lançamentos
    ConferenciaSomaPeriodo ..> Transacao : soma as gravadas e as novas
    ImportacaoArquivoFactory ..> ImportacaoArquivo : registra
    LoteTransacoesFactory ..> ImportacaoArquivo : liga as transações
    LoteTransacoesFactory ..> Lancamento : lê
    LoteTransacoesFactory ..> ChaveDeduplicacao : calcula pelo lote
    LoteTransacoesFactory ..> Transacao : cria
    DespesaParceladaFactory ..> Lancamento : lê
    DespesaParceladaFactory ..> DespesaParcelada : cria as que faltam
    DespesaAdiadaFactory ..> Transacao : lê as novas
    DespesaAdiadaFactory ..> DespesaAdiada : cria as cobradas sem previsão
```

## Serviços de domínio

Puros, sem infraestrutura: recebem os agregados e as listas de que precisam, e quem chama os carrega pelos repositórios. Uma lista com item de outro usuário é erro de quem chama e lança `ArgumentException`.

| Serviço | O que faz |
|---|---|
| `AliasesBeneficiarioPendentes` | Lista os textos de beneficiário das saídas de um arquivo que ainda não são alias de nenhum beneficiário do usuário, um por forma normalizada, para a prévia da importação (`Listar`), e falha enquanto houver algum (`Conferir`, com `ErroBeneficiariosNaoResolvidos`), o que a `LoteTransacoesFactory` usa para recusar o lote. |
| `AliasesCategoriaPendentes` | Lista as categorias do banco de um arquivo que ainda não são alias de nenhuma categoria do usuário, uma por forma normalizada, para a prévia da importação (`Listar`), e falha enquanto houver alguma (`Conferir`, com `ErroCategoriasBancoNaoResolvidas`), o que a `LoteTransacoesFactory` usa para recusar o lote. Olha todo lançamento, inclusive a entrada e o estorno. |
| `BalancoMensalService` | Calcula o `BalancoMensal` de um mês (`Calcular`): a renda líquida atual do usuário e os totais pago e pendente dos pagamentos das despesas que caem nele (`PagamentoDoMes`, pelo `ValorVigenteDoMes`): a despesa do cartão cobrada e não paga entra no pendente pelo valor cobrado. A renda não tem histórico, e despesa sem pagamento no mês lança `ArgumentException`. |
| `BeneficiarioFactory` | Aplica um lote inteiro ou nada de beneficiários novos, com ou sem alias, e aliases para os existentes (`Resolver`), conferindo que cada alias aponta para um beneficiário só entre os dele e os do lote. |
| `CategoriaDespesaFactory` | Cria a categoria Outros do usuário (`CriarOutros`), aplica um lote inteiro ou nada de categorias novas e aliases para as existentes (`Resolver`), conferindo o nome e os aliases entre as categorias dele e as do lote, e renomeia uma categoria (`Renomear`), conferindo o nome entre as dele. |
| `CartaoFactory` | Cria o cartão do usuário, conferindo banco + sufixo entre os cartões dele, ou obtém os cartões dos finais de uma fatura (`ObterDosFinais`), falhando quando falta algum. `ObterDoFinal` acha o cartão de um final, para a prévia da importação. |
| `ContaBancariaFactory` | Acha a conta do extrato entre as do usuário, ou cria uma. |
| `DespesaParceladaFactory` | Cria as despesas parceladas que as compras de uma fatura revelam e que o usuário ainda não tem (`CriarDaFatura`), reconhecendo a mesma compra pela `ChaveCompraParcelada`. Recusa o lançamento que não é de fatura. Só cria, nunca altera. |
| `DespesaAdiadaFactory` | Cria, das compras à vista novas de uma fatura, as despesas adiadas já cobradas e pendentes que nenhuma adiada prevista cobre (`CriarDaFatura`); cada previsão (`DespesaAdiada.EhPrevisaoDe`) cobre uma compra só, também de outro final da mesma fatura. Recusa a transação que não é de fatura. Só cria, nunca altera: a prevista continua sem cobrança. |
| `VinculoTransacaoService` | Liga uma saída a uma despesa (`Vincular`), conferindo se a transação já é usada (a despesa que a usa vem pela cobrança, quando ela é do cartão, ou pelo pagamento, quando é da conta), se é saída, a origem e, na parcelada, a compra pela `ChaveCompraParcelada`. A simples é paga pela saída da conta, na data da transação; a adiada é cobrada pela compra de qualquer cartão da fatura em que vence (`FaturaCartao`, com os cartões do usuário), e a parcela, pela compra do cartão dela, sem pagar. Beneficiário, janela de data, proximidade de valor e mês da parcela não bloqueiam. Desfaz pela origem da transação (`Desvincular`): o cartão tira a cobrança, e a conta, o pagamento. Chamado pelos endpoints de vínculo e de desvínculo e pelas ferramentas MCP `prever_vinculo`, que aplica o vínculo em memória e não grava, e `confirmar_vinculo`, que o refaz e grava depois que o usuário aprova o resumo no cliente. |
| `PagamentoFaturaService` | Paga a fatura com uma saída da conta (`Pagar`): recebe a saída, um cartão, o mês de vencimento, os cartões do usuário, as despesas do mês e as que a saída já pagou, e paga as cobradas e pendentes dos cartões da mesma fatura naquele mês, com a data da saída e o valor cobrado. Recusa a entrada, a saída do cartão, a saída que já paga uma simples (`ErroTransacaoJaVinculada`) ou outra fatura, e a fatura sem nada a pagar. Devolve o `PagamentoFatura`, com as despesas pagas na chamada, o mês de vencimento e o total pago (`Total`, a soma do valor cobrado). Desfaz o pagamento (`Desfazer`): recebe a saída e as despesas que ela pagou, e devolve a pendente as do cartão, com a cobrança preservada. Chamado pelos endpoints de pagar e de desfazer o pagamento da fatura e pelas ferramentas MCP `prever_pagamento_fatura`, que aplica o pagamento em memória e não grava, e `confirmar_pagamento_fatura`, que o refaz e grava depois que o usuário aprova o resumo no cliente. |
| `ConciliacaoParcelasService` | Cobra cada saída parcelada livre, no cartão, na parcela de mesmo número da mesma compra (`Conciliar`), pelo `VinculoTransacaoService`. Entre gêmeas, casa primeiro o valor igual ao projetado e depois a ordem de valor (`EmparelhamentoParcelas`), com desempate pelo id. A transação sem parcela livre conta em `SemParcela` e é tentada de novo na próxima chamada; entrada, valor zero e transação sem parcela, como toda a da conta, são ignorados. Só vincula: não cria despesa nem altera transação. |
| `AlteracaoCategoriaDespesaService` | Troca a categoria de uma despesa, conferindo que a despesa e a categoria são do mesmo usuário. |
| `DespesaRecorrenteFactory` | Cria a despesa recorrente no crédito (`CriarNoCredito`) ou sem cartão (`CriarSemCartao`), conferindo que não repete beneficiário e descrição de uma recorrente do usuário. |
| `ImportacaoArquivoFactory` | Registra a importação de um arquivo com o usuário e a origem da própria conta ou do próprio cartão. |
| `GeracaoDespesasDoMesService` | Gera as despesas de um mês a partir das recorrentes, ligadas a elas: `DespesaSimples` pendente, com o pagamento projetado no dia da ocorrência, ou `DespesaAdiada` com pagamento no vencimento do cartão no mês pedido e a ocorrência no mês da compra que cai nessa fatura (`Cartao.DataOcorrenciaQueVenceEm`). Recebe as despesas do usuário no mês e gera só para as recorrentes que ainda não têm uma, então gerar o mesmo mês de novo não duplica; o índice único `(id_despesa_recorrente, mes_referencia)` é a rede. |
| `LoteTransacoesFactory` | Confere que a origem é do usuário, que a importação é da mesma origem, que os lançamentos são do mesmo tipo de origem, que nenhum id de banco se repete no lote, que todo texto de beneficiário já é alias de um beneficiário do usuário e que toda categoria do banco já é alias de uma categoria dele; calcula as chaves e cria as transações ligadas à importação. |
| `ConferenciaSomaPeriodo` | Confere, antes de gravar, que as transações do período (as já gravadas e as novas) somam o mesmo que os lançamentos do extrato. A divergência é aviso. |

## Portas

| Porta | Agregado |
|---|---|
| `IUsuarioRepository` | `Usuario` |
| `IBeneficiarioRepository` | `Beneficiario` (lista os do usuário atual, obtém um dele) |
| `ICartaoRepository` | `Cartao` (lista os do usuário, obtém um do usuário) |
| `IContaRepository` | `ContaBancaria` (lista as do usuário) |
| `ITransacaoRepository` | `Transacao` (obtém uma pelo id, lista as com parcela, lista as de um mês em ordem de data, adiciona só as de chave nova e devolve essas mesmas instâncias) |
| `IImportacaoRepository` | `ImportacaoArquivo` |
| `IDespesaRecorrenteRepository` | `DespesaRecorrente` (lista as do usuário atual) |
| `IDespesaRepository` | `Despesa` (lista as do usuário atual num mês de referência, inclusive a parcelada com parcela nele, lista as parceladas dele, obtém uma pelo id ou pela compra que a cobrou, lista as que uma transação pagou, adiciona as novas; a extensão `ObterQueUsaAsync` escolhe entre as duas buscas pela origem da transação) |

## Value objects

Cada value object mora na pasta do agregado dono, mesmo quando outro agregado o usa:

- `Cartoes`: `SufixoCartao`, `DiaVencimentoCartao`, `DiaFechamentoCartao`, `VencimentoFatura` (o mês e o dia de vencimento de uma fatura, ordenado pela data e convertido implicitamente para `DateOnly`) e `FaturaCartao` (os cartões que saem na mesma fatura e o vencimento dela; a igualdade não depende da ordem dos cartões).
- `Contas`: `SufixoConta` e o SmartEnum `TipoConta`.
- `Despesas`: `DiaOcorrenciaRecorrente`, `Pagamento` (valores projetado e efetivo, `StatusPagamento`, data projetada, sempre presente, e efetiva, o mês de referência, que é o da data projetada e não muda ao pagar nem ao reabrir, e a transação que o pagou; o `CK_despesas_pagamento_mes` e o `CK_parcelas_despesa_pagamento_mes` repetem o mês no banco), `Cobranca` (a compra do cartão que cobrou a despesa adiada ou a parcela e o valor que ela fixou; os CHECKs fazem as duas colunas andarem juntas, só na adiada e na parcela, e exigem a cobrança na despesa do cartão paga por transação), `ChaveCompraParcelada`, `ParcelaDaCompra` (a chave e o número, que agrupam a conciliação) e os SmartEnums `FormaPagamento` e `StatusPagamento`.
- `Importacoes`: `Extrato`, `LancamentoContabilizado`, `Fatura`, `LancamentoFatura`, `PeriodoArquivo`, `SaldoDiario` e o SmartEnum `LayoutArquivoImportacao`, com as listas `Extratos` (o arquivo traz a conta), `ExtratosContaInformada` (o usuário informa o sufixo, numa rota própria do layout) e `Faturas`.
- `Transacoes`: `OrigemTransacao`, `Lancamento`, `Parcela`, `IdentidadeLancamento`, `ChaveDeduplicacao`.

Em `Shared/ValueObjects` ficam só os conceitos sem dono: `Reais` (com sinal, e o zero sempre sem sinal) e `ReaisPositivo` para dinheiro; `TextoObrigatorio`; `MesReferencia`, o calendário do balanço, nos anos de 1 a 9999 que o `DateOnly` representa; `BalancoMensal`, o balanço de um mês, com a renda líquida, os totais pago e pendente (`ReaisPositivo`) e o `SaldoPrevisto`, a renda menos os dois, que só o `BalancoMensalService` monta; `DiaValidoEmTodoMes`, a base dos dias de 1 a 28 que o `MesReferencia` aceita; o SmartEnum `Banco` (código COMPE); `TextoNormalizado`, a forma de um texto usada para comparar (aparado, espaços juntos, sem acentos, em maiúsculas), que o `Alias` e o nome da `CategoriaDespesa` usam; `Alias`, um texto do arquivo com o original, para exibir, e o normalizado, que decide a igualdade; `Alfanumerico`, só as letras e os dígitos ASCII de um texto; `DocumentoMascarado`, um texto do banco com os CPFs e CNPJs de verificadores válidos trocados por `[CPF]` e `[CNPJ]`, e que diz se sobrou conteúdo além dos rótulos; e o `Documento` abstrato, com o `Cpf` e o `Cnpj`.

- **Documento**: guardado sem a máscara e em maiúsculas, com os dígitos verificadores conferidos (módulo 11). O `Documento.Criar` escolhe pelo tamanho: 11 caracteres é `Cpf`, e 14 é `Cnpj`. O `Cnpj` segue a regra alfanumérica vigente desde julho de 2026, e o CNPJ só numérico é um caso particular dela. As mensagens de falha não repetem o número. O `Cpf` e o `Cnpj` servem ao `DocumentoMascarado`, que só troca os documentos de verificadores válidos. Os leitores mantêm os documentos na descrição e na contraparte, que são gravadas e exibidas assim; a máscara é aplicada ao que sair rumo ao modelo.
