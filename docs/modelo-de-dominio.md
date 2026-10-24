# Modelo de domínio

Retrato do `src/ControleFinanceiro.Domain`: agregados, entidades, value objects e como se referenciam. Mostra o que é estável (estrutura, relações, invariantes principais): as classes não listam propriedades, só os métodos que carregam regra de negócio. **Quem muda agregado, entidade, value object ou relação no Domain atualiza este arquivo no mesmo PR**; o `revisor-dominio` cobra.

Legenda:

- `<<AggregateRoot>>`: raiz de agregado, carregada e gravada inteira pelo seu repositório.
- `<<Entity>>`: entidade interna de um agregado, só alcançada pela raiz.
- `<<ValueObject>>` e `<<SmartEnum>>`: sem identidade, comparados pelo valor.
- `*--` composição (faz parte do agregado); `..>` referência por id a outro agregado; `--|>` herança.
- Ids são structs Vogen sobre `Guid` (`IdUsuario`, `IdDespesa`…), gerados com `FromNewVersion7Guid`.

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
        }
    }

    namespace Contas {
        class ContaBancaria {
            <<AggregateRoot>>
        }
    }

    namespace Despesas {
        class DespesaRecorrente {
            <<AggregateRoot>>
        }
        class Despesa {
            <<AggregateRoot, abstract>>
        }
        class DespesaInteira {
            <<abstract>>
        }
        class DespesaSimples
        class DespesaAdiada
        class DespesaParcelada
        class ParcelaDespesa {
            <<Entity>>
        }
    }

    namespace Beneficiarios {
        class Beneficiario {
            <<AggregateRoot>>
            AdicionarAlias()
        }
        class AliasBeneficiario {
            <<ValueObject>>
        }
    }

    namespace Categorias {
        class CategoriaDespesa {
            <<AggregateRoot>>
            Renomear()
        }
    }

    namespace Transacoes {
        class Transacao {
            <<AggregateRoot>>
        }
        class OrigemTransacao {
            <<ValueObject>>
        }
    }

    namespace Importacoes {
        class ImportacaoExtrato {
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
    DespesaParcelada "1" *-- "*" ParcelaDespesa
    Despesa ..> Usuario : IdUsuario
    Despesa ..> Beneficiario : IdBeneficiario
    Despesa ..> CategoriaDespesa : IdCategoria

    Beneficiario "1" *-- "*" AliasBeneficiario
    Beneficiario ..> Usuario : IdUsuario (long)
    CategoriaDespesa ..> Usuario : IdUsuario (long)

    Transacao *-- OrigemTransacao
    Transacao ..> Usuario : IdUsuario
    Transacao ..> ImportacaoExtrato : IdImportacao
    OrigemTransacao ..> ContaBancaria : IdConta
    OrigemTransacao ..> Cartao : IdCartao
    ImportacaoExtrato *-- OrigemTransacao
    ImportacaoExtrato ..> Usuario : IdUsuario
```

### Invariantes que valem saber

- **Usuario**: `RendaLiquida` = renda bruta − descontos.
- **Cartao**: um por banco + sufixo em cada usuário. Só nasce pela `CartaoFactory`, que recebe os cartões do usuário e falha com `ErroCartaoJaCadastrado`; o índice único da tabela é a rede para duas requisições ao mesmo tempo.
- **ContaBancaria**: uma por banco + sufixo em cada usuário, com o apelido padrão "&lt;tipo&gt; &lt;banco&gt; final &lt;sufixo&gt;". Só nasce pela `ContaBancariaFactory.ObterOuCriar`, que devolve a conta existente ou uma nova, e falha com `ErroContaDeOutroTipo` se a conta existe com outro tipo.
- **DespesaRecorrente**: cartão obrigatório no crédito e proibido nas outras formas. Só nasce pela `DespesaRecorrenteFactory`, que confere que o cartão é do próprio usuário.
- **Despesas**: `DespesaSimples` é paga na compra e não aceita crédito; `DespesaAdiada` é sempre crédito, com pagamento pendente no mês seguinte, no dia de vencimento do cartão; `DespesaParcelada` é sempre crédito, com uma `ParcelaDespesa` pendente por mês.
- **Transacao**: valor com sinal do ponto de vista do usuário (negativo é saída). Só nasce pela `LoteTransacoesFactory`, que confere que a conta ou o cartão é do usuário e que a importação é da mesma origem, e garante a `ChaveDeduplicacao` do lote. A identidade é o `IdTransacao`; a chave só reconhece o lançamento numa reimportação. Guarda a importação que a criou: a reimportação não a recria, então ela fica ligada à primeira importação que a trouxe.
- **OrigemTransacao**: conta ou cartão, nunca os dois.
- **ImportacaoExtrato**: só nasce pela `ImportacaoExtratoFactory`, que tira o usuário e a origem da própria conta ou do próprio cartão, antes do lote de transações. Quantas eram novas não fica nela: são as transações ligadas a ela. `PeriodoNaoImportado` aponta o buraco entre a importação anterior da mesma origem e esta.

### Erros

Cada falha prevista que é uma situação de negócio com nome próprio é um tipo derivado do `Erro` abstrato, na pasta do seu agregado, com igualdade só pelo `Codigo`; quem chama decide pelo tipo, e o endpoint mapeia cada tipo para um status. Valor que não pertence ao conceito, como a validação de um value object, falha com mensagem em texto (`Result<T>`).

| Erro | Agregado | Quando |
|---|---|---|
| `ErroCartaoJaCadastrado` | `Cartoes` | O usuário já tem um cartão do banco com o mesmo final. |
| `ErroContaDeOutroTipo` | `Contas` | O usuário já tem a conta, mas de outro tipo. |
| `ErroOrigemNaoPertenceAoUsuario` | `Transacoes` | A conta ou o cartão do lote é de outro usuário. |
| `ErroIdentificadorRepetido` | `Transacoes` | Dois lançamentos do lote têm o mesmo id do banco. |

## Importação de um extrato

O extrato e os seus lançamentos são value objects: não têm identidade nem são gravados, mas são linguagem do negócio. O adaptador de cada layout produz o `Extrato`, a `ImportacaoExtratoFactory` registra a importação, a `LoteTransacoesFactory` transforma os lançamentos em transações ligadas a ela e a `ConferenciaSomaPeriodo`, antes da gravação, compara a soma do período com o que já estava gravado mais as novas.

- **Extrato**: só nasce com os saldos fechando. O `Criar` falha quando um dia contábil traz mais de um saldo, quando falta o saldo de um dia ou quando os lançamentos de um dia não levam o saldo do dia anterior ao saldo do dia. Agrupa pela data contábil; o primeiro dia só serve de base, e os dias contábeis depois do fim do período ficam de fora. As mensagens citam só as datas.
- **Lancamento**: a linha de extrato ou de fatura que vira transação. Duas linhas com os mesmos campos são iguais como valor; quem as distingue é a posição no lote, na `ChaveDeduplicacao`.
- **LancamentoContabilizado**: o lançamento com o dia contábil em que entrou no saldo. A data contábil só serve à conferência de saldo do `Extrato` e não é gravada.

```mermaid
classDiagram
    direction LR

    class Extrato {
        <<ValueObject>>
        Criar()
    }
    class LancamentoContabilizado {
        <<ValueObject>>
    }
    class Lancamento {
        <<ValueObject>>
    }
    class IdentidadeLancamento {
        <<ValueObject>>
        Unica()
        Composta()
    }
    class SaldoDiario {
        <<ValueObject>>
    }
    class PeriodoExtrato {
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
    class ImportacaoExtratoFactory {
        <<service>>
        Registrar(ContaBancaria ou Cartao, layout, periodo, lidas, importadaEm)
    }
    class LoteTransacoesFactory {
        <<service>>
        Criar(IdUsuario, ContaBancaria ou Cartao, ImportacaoExtrato, lancamentos)
    }
    class ImportacaoExtrato {
        <<AggregateRoot>>
    }
    class Transacao {
        <<AggregateRoot>>
    }

    Extrato "1" *-- "*" LancamentoContabilizado
    Extrato "1" *-- "*" SaldoDiario
    Extrato *-- PeriodoExtrato
    LancamentoContabilizado *-- Lancamento
    Lancamento *-- IdentidadeLancamento
    ConferenciaSomaPeriodo ..> Extrato : soma os lançamentos
    ConferenciaSomaPeriodo ..> Transacao : soma as gravadas e as novas
    ImportacaoExtratoFactory ..> ImportacaoExtrato : registra
    LoteTransacoesFactory ..> ImportacaoExtrato : liga as transações
    LoteTransacoesFactory ..> Lancamento : lê
    LoteTransacoesFactory ..> ChaveDeduplicacao : calcula pelo lote
    LoteTransacoesFactory ..> Transacao : cria
```

## Serviços de domínio

Estáticos e puros: recebem os agregados e as listas de que precisam, e quem chama os carrega pelos repositórios. Uma lista com item de outro usuário é erro de quem chama e lança `ArgumentException`.

| Serviço | O que faz |
|---|---|
| `CartaoFactory` | Cria o cartão do usuário, conferindo banco + sufixo entre os cartões dele. |
| `ContaBancariaFactory` | Acha a conta do extrato entre as do usuário, ou cria uma. |
| `DespesaRecorrenteFactory` | Cria a despesa recorrente, conferindo que o cartão é do usuário. |
| `ImportacaoExtratoFactory` | Registra a importação de um arquivo com o usuário e a origem da própria conta ou do próprio cartão. |
| `GeracaoDespesasDoMesService` | Gera as despesas de um mês a partir das recorrentes: `DespesaSimples` com pagamento imediato, ou `DespesaAdiada` com pagamento no vencimento do cartão. |
| `LoteTransacoesFactory` | Confere que a origem é do usuário, que a importação é da mesma origem e que nenhum id de banco se repete no lote; calcula as chaves e cria as transações ligadas à importação. |
| `ConferenciaSomaPeriodo` | Confere, antes de gravar, que as transações do período (as já gravadas e as novas) somam o mesmo que os lançamentos do extrato. A divergência é aviso. |

## Portas

| Porta | Agregado |
|---|---|
| `IUsuarioRepository` | `Usuario` |
| `ICartaoRepository` | `Cartao` (lista os do usuário) |
| `IContaRepository` | `ContaBancaria` (lista as do usuário) |
| `ITransacaoRepository` | `Transacao` (adiciona só as de chave nova e devolve essas mesmas instâncias) |
| `IImportacaoRepository` | `ImportacaoExtrato` |

O repositório da `DespesaRecorrente` chega com o cadastro das despesas fixas (#9).

## Value objects

Cada value object mora na pasta do agregado dono, mesmo quando outro agregado o usa:

- `Cartoes`: `SufixoCartao`, `DiaVencimentoCartao`.
- `Contas`: `SufixoConta` e o SmartEnum `TipoConta`.
- `Despesas`: `DiaCompraRecorrente`, `Pagamento` (valor, `StatusPagamento`, datas projetada e efetiva) e os SmartEnums `FormaPagamento` e `StatusPagamento`.
- `Importacoes`: `Extrato`, `LancamentoContabilizado`, `PeriodoExtrato`, `SaldoDiario` e o SmartEnum `LayoutArquivoImportacao`.
- `Transacoes`: `OrigemTransacao`, `Lancamento`, `IdentidadeLancamento`, `ChaveDeduplicacao`.

Em `Shared/ValueObjects` ficam só os conceitos sem dono: `Reais` (com sinal) e `ReaisPositivo` para dinheiro; `TextoObrigatorio`; `MesReferencia`, o calendário do balanço; `DiaValidoEmTodoMes`, a base dos dias de 1 a 28 que o `MesReferencia` aceita; e o SmartEnum `Banco` (código COMPE).
