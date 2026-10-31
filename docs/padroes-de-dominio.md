# Padrões de domínio

Como modelar no `src/ControleFinanceiro.Domain`. Vale para toda feature nova e para toda mudança num agregado. O [modelo de domínio](modelo-de-dominio.md) retrata o que existe; este guia diz como o que vier deve ser feito. O `planejador` cita a seção que cada decisão do plano segue, e o `revisor-dominio` cobra.

Discordar de um padrão é motivo para conversar, não para contorná-lo no código. Cada regra cita um exemplo do repositório: leia o exemplo antes de seguir a regra.

## 1. Fronteiras do agregado

- A raiz é a única entrada do agregado. Entidade interna (`DescontoSalarial` no `Usuario`, `ParcelaDespesa` na `DespesaParcelada`) só é criada, alterada e lida pela raiz.
- Entre agregados, a referência é **pelo id da raiz**. Nenhum agregado guarda outra raiz, nem uma entidade de outro agregado, nem uma coleção delas. A `DespesaRecorrente` guarda o `IdCartao`, não o `Cartao`; a `OrigemTransacao` guarda o `IdConta` ou o `IdCartao`.
- Um conceito que outro agregado precisa referenciar de fora é uma raiz, não uma entidade interna. Foi o motivo da #50: o `Cartao` e a `ContaBancaria` eram internos do `Usuario`, mas a `Transacao` e a `ImportacaoArquivo` já chegavam a eles de fora.
- Todo agregado que pertence a um usuário tem `IdUsuario` e implementa `IPertenceAoUsuario` (`Cartao`, `ContaBancaria`, `DespesaRecorrente`, `Despesa`, `Transacao`, `ImportacaoArquivo`, `Beneficiario`, `CategoriaDespesa`), mesmo quando o dono já é alcançável por outro id, como a origem da transação. Toda consulta dele fica restrita ao usuário atual (seção 8); sem a interface, o filtro some em silêncio, e o `PertenceAoUsuarioTests` é o que avisa. O `Usuario` é o dono e não a implementa.

## 2. Um tipo de agregado por transação

Um caso de uso altera agregados de um tipo só e grava com um `CommitAsync`. Ele pode ler agregados de outros tipos e criar um agregado novo a partir deles, como o cadastro de cartão, que lê os cartões do usuário e cria um.

Várias instâncias do mesmo tipo no mesmo caso de uso:

- **Criar várias é livre.** Nenhuma tem estado anterior para disputar, e a regra entre elas (unicidade) fica com a factory e o índice único (seção 3). Exemplo: a geração das despesas do mês a partir das recorrentes.
- **Alterar várias é permitido quando cada alteração é independente:** a mesma operação aplicada a cada instância, e cada uma fica correta sozinha, mesmo que as outras não mudem. Exemplo: marcar várias despesas como pagas de uma vez.
- **Alterar instâncias que dependem umas das outras pede justificativa no plano.** Se a operação só fica correta com as duas mudanças juntas, como tirar um alias de um beneficiário e pôr em outro, existe uma regra entre as instâncias que nenhuma delas protege sozinha. Antes de aceitar, tente nesta ordem:
  1. modelar a operação alterando uma instância só. Exemplo: mesclar um beneficiário em outro grava no mesclado o id do destino, e os aliases do mesclado passam a ser lidos como do destino, sem mover nada;
  2. ver se a regra pede um agregado que contenha as instâncias, ou se a segunda pode mudar depois, por evento (seção 6);
  3. se nada disso servir, aceitar a exceção e registrar o motivo no plano.

  A regra que envolve as instâncias segue a seção 3: mora num método do agregado que recebe a outra instância do mesmo tipo, ou numa factory quando depende de mais que as duas.

O motivo de vigiar o último caso não é desempenho (o sistema tem um usuário e um banco só), e sim o lugar da regra: uma invariante entre instâncias que só o caso de uso conhece se perde no próximo caso de uso que mexer nelas.

Precisar alterar agregados de dois tipos no mesmo caso de uso é sinal de que o efeito sobre o segundo tipo é um evento de domínio (seção 6), ou de que a fronteira está errada. Pare e discuta no plano antes de escrever código.

**Exceção: criar vários agregados novos nascidos do mesmo fato.** Um caso de uso que só cria agregados, sem alterar nenhum que já existe, pode gravá-los num `CommitAsync` só. É a importação de um extrato: a conta nova (quando o arquivo é o primeiro dela), as transações e a `ImportacaoArquivo` nascem do mesmo arquivo e são gravadas juntas, para o arquivo entrar inteiro ou não entrar. Criar não disputa estado com outra requisição; a unicidade fica com o índice único (seção 3).

## 3. Regras que cruzam agregados

Uma regra que depende de mais de um agregado, como "um cartão por banco e final em cada usuário", não mora em nenhum dos agregados nem no endpoint. Ela mora numa **factory ou serviço de domínio estático e puro**, em `Domain/Services/`:

- Recebe os agregados e as listas de que precisa e devolve `Result`. É síncrona, não recebe porta e se testa sem mock. Exemplos: `CartaoFactory`, `ContaBancariaFactory`, `DespesaRecorrenteFactory`, `ImportacaoArquivoFactory` e `LoteTransacoesFactory`.
- Quem chama (o caso de uso, hoje o endpoint) carrega os dados pelos repositórios e passa à factory.
- Uma lista que traz item de outro usuário é bug de quem chama, não falha prevista: lança `ArgumentException`. O mesmo vale para um argumento que contradiz o estado já conferido, como o cartão errado no `GeracaoDespesasDoMesService.GerarComPagamentoAdiado`.
- Quando só a factory consegue garantir a regra, o `Criar` do agregado é `internal`, e ela é a única entrada. Os testes passam pela factory, sem `InternalsVisibleTo`, e o AutoFixture ganha um generator que a chama (`CartaoGenerator`). Exemplos: `Cartao.Criar`, `ContaBancaria.Criar`, `DespesaRecorrente.Criar`, `ImportacaoArquivo.Registrar` e `Transacao.Criar`.
- **Um agregado não recebe agregado de outro tipo nos próprios métodos.** A regra que precisa dos dois vai para a factory, que passa ao agregado só o id. A `DespesaRecorrente.Criar` recebe o `IdCartao`; a `DespesaRecorrenteFactory` recebe o `Cartao` e confere que ele é do usuário. Receber uma instância do mesmo tipo é aceito, como a `ImportacaoArquivo.PeriodoNaoImportado`, que lê a importação anterior da mesma origem.
- Um parâmetro que às vezes é ignorado esconde a regra: prefira uma sobrecarga por caso, com o nome do caso (`GerarComPagamentoImediato` e `GerarComPagamentoAdiado`).

Concorrência: a unicidade garantida pela factory tem um índice único no banco como rede. Duas requisições ao mesmo tempo dão 500 na segunda, e isso é aceito até haver motivo para tratar. A exceção é a `DespesaRecorrente` repetida (mesmo beneficiário e mesma descrição, sem contar caixa, acentos e espaços repetidos): só a factory a recusa, sem índice, e duas requisições ao mesmo tempo podem gravar as duas. O mesmo vale para a `DespesaParcelada` da mesma compra, recusada só pela `DespesaParceladaFactory`, porque a chave depende de texto normalizado, que não é coluna.

## 4. Repositórios

- Um repositório por raiz, com a porta no Domain, na pasta do agregado (`Cartoes/ICartaoRepository.cs`) e o adaptador na Persistence (`Cartoes/CartaoRepository.cs`).
- O repositório **recebe e devolve o agregado inteiro**: para adicionar (`AdicionarAsync`), para obter e alterar (`ObterAsync`, seguido do `CommitAsync` do `IUnitOfWork`) e para listar os do usuário atual (`ListarAsync`). O agregado vem com todas as coleções (`AutoInclude`).
- A porta de agregado do usuário **não recebe o `IdUsuario`**: quem é o usuário atual vem do `IUsuarioAtual`, pelo filtro do `DbContext`. O item de outro usuário é "não encontrado", como o que não existe. O `IUsuarioRepository` segue com `ObterPorIdAsync`, porque o `Usuario` fica fora do filtro.
- O repositório de agregado não devolve DTO, projeção, contagem nem existência. Uma consulta que não é "me dê o agregado", como uma contagem ou um relatório, vai para:
  - um **serviço de domínio não puro** com uma porta de consulta própria, quando a resposta alimenta uma regra de domínio;
  - ou um **serviço de aplicação**, na Application, quando a resposta é só para quem chama (tela, MCP, relatório).
- Um agregado sem caso de uso não ganha repositório antes da hora.

## 5. Falhas

Falha prevista é `Result`; exceção fica para erro de sistema e para bug de quem chama. O domínio decide a forma do erro pela linguagem do negócio, não por quem o consome:

- **Erro tipado** (`Result<T, Erro>`) para cada falha que é uma situação de negócio com nome próprio (o cartão já cadastrado, a conta de outro tipo), com ou sem dados. Cada uma é um tipo derivado do `Erro`, na pasta do agregado, com igualdade pelo código. Exemplos: `ErroCartaoJaCadastrado`, `ErroContaDeOutroTipo` (com o `TipoExistente`) e `ErroIdentificadorRepetido` (com as `Posicoes`).
  - Erro sem parâmetros é uma instância única (`ErroOrigemNaoPertenceAoUsuario.Instancia`); com parâmetros, nasce com `new` a cada falha.
  - O construtor é `internal`: só o domínio cria o erro.
  - Quem chama compara pelo tipo (`erro is ErroCartaoJaCadastrado`), nunca pelo `Codigo` ou pela `Descricao`.
- **Mensagem em texto** (`Result<T>`) para valor que não pertence ao conceito, como a validação de um value object: todas essas falhas são a mesma situação, entrada inválida.

As camadas de fora reagem aos tipos (o endpoint mapeia cada um para um status num `switch` só, como no `AdicionarCartaoEndpoint`), mas não são motivo para criá-los nem para removê-los. O tipo nasce quando a situação de negócio aparece, não antes.

## 6. Efeito colateral em outro agregado

Quando alterar um agregado precisa mudar outro de tipo diferente (a seção 2 proíbe fazer os dois no mesmo caso de uso), o efeito vai por **evento de domínio**: o primeiro agregado registra que algo aconteceu, e um tratador altera o segundo, depois, na sua própria transação. A consistência entre os dois é eventual.

**Esse mecanismo ainda não existe.** No primeiro caso real, abra uma issue para desenhá-lo antes de implementar. O ponto de partida:

- a raiz registra os eventos numa lista própria, preenchida pelos métodos que mudam o estado;
- os eventos são despachados depois do commit, para que um tratador nunca veja estado que não foi gravado;
- o despacho começa dentro do próprio processo. Se perder um evento não for aceitável, os eventos vão para uma tabela de outbox na mesma transação do agregado e são publicados depois;
- um broker (RabbitMQ, Kafka ou parecido) só entra quando houver outro processo para consumir os eventos.

Até lá, uma feature que precise disso para no plano.

## 7. Value objects e ids

- **Primitivo com regra vira value object.** Dinheiro é `Reais` (com sinal) ou `ReaisPositivo`, nunca `decimal` solto. O mesmo vale para todo primitivo com formato, faixa, obrigatoriedade ou unidade: o value object valida no `Criar`, que devolve `Result<T>` (`SufixoCartao`, `DiaVencimentoCartao`, `TextoObrigatorio`). O validator do endpoint pode repetir a regra para responder 400, mas chamando o `Criar` (`MustBeValueObject`).
- O value object mora **na pasta do agregado dono**, mesmo quando outro agregado o usa; esse outro passa a referenciar o namespace do dono. `Shared/ValueObjects` fica só com os conceitos sem dono, usados por vários agregados (`Reais`, `TextoObrigatorio`, `Banco`, `MesReferencia`).
- `Shared` não depende de pasta de agregado. Se um tipo de `Shared` precisa aceitar value objects de agregados diferentes, ele recebe uma base comum em `Shared`, como o `DiaValidoEmTodoMes` do `MesReferencia.NoDia`.
- Ids são structs Vogen sobre `Guid`, gerados no domínio com `FromNewVersion7Guid`, com conversor EF registrado. Sem `Guid` solto na API pública do domínio.

## 8. Persistência dos limites

- O mapeamento fica em `IEntityTypeConfiguration` na Persistence, na pasta do agregado (`Cartoes/CartaoConfiguration.cs`). O domínio não conhece EF.
- A FK entre agregados é declarada **sem navegação**: `HasOne<Usuario>().WithMany().HasForeignKey(c => c.IdUsuario)`, com o `IdUsuario` como propriedade real do agregado. Navegação de coleção só para entidades internas, sempre com `AutoInclude`.
- Toda raiz que implementa `IPertenceAoUsuario` ganha, no `ControleFinanceiroDbContext`, o filtro global nomeado `FiltroUsuario` (`IdUsuario == usuário atual`), montado num laço sobre o modelo: agregado novo mapeado já chega filtrado, sem tocar no `DbContext`. O repositório não repete o `Where` por usuário. Só a leitura de volta dos testes de integração usa `IgnoreQueryFilters`, porque roda fora de requisição e confere a tabela inteira.
- Cascade entre agregados é exceção: só quando o filho não faz sentido sem o pai e não existe exclusão que dependa de regra (hoje, cartões de um usuário que não pode ser excluído).

## 9. Checklist para o plano

Responda no plano, para cada conceito novo ou alterado:

1. É raiz ou entidade interna? Algum outro agregado vai referenciá-lo? (seção 1)
2. Que ids ele guarda de outros agregados? (seção 1)
3. Que regra depende de mais de um agregado, e em que factory ou serviço ela mora? O `Criar` precisa ser `internal`? (seção 3)
4. Que caso de uso altera agregados de mais de um tipo? Se algum, qual evento substitui a alteração do segundo tipo? (seções 2 e 6)
5. Que repositório o caso de uso usa, e que consulta não é "me dê o agregado"? (seção 4)
6. Que falhas são situações de negócio com nome próprio, e quais são entrada inválida? (seção 5)
7. Que primitivos com regra viram value object, e em que pasta? (seção 7)
8. Como fica a FK entre os agregados? (seção 8)
