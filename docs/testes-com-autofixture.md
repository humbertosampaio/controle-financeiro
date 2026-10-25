# Testes com AutoFixture

O AutoFixture existe aqui para uma coisa: **dar ao teste um objeto válido sem que ele precise saber os valores de dentro.** Um teste do cadastro de cartão precisa de um `Usuario` cadastrado, mas não liga para o nome nem para a renda. Se o teste montasse o usuário à mão, um `"Maria"` e um `5000m` apareceriam no código como se importassem para o resultado.

Por isso, a regra é: **o que o teste não verifica, o AutoFixture gera; o que ele verifica, o teste declara.**

## Como um objeto válido nasce

Os value objects só existem válidos (`SufixoCartao.Criar` falha para algo que não sejam 4 dígitos, `DiaVencimentoCartao.Criar` falha fora de 1 a 28). O AutoFixture, sozinho, não conhece essas regras: geraria `"a3f9…"` para o sufixo e `173` para o dia. As customizações em `tests/ControleFinanceiro.Tests.Shared/AutoFixture/Customizations/Domain/Shared/` ensinam a regra a ele, uma por value object, e o `DomainFixture` reúne todas.

Value object novo com regra pede customização nova no `DomainFixture`. Sem ela, qualquer teste que peça o tipo, ou uma entidade que o contenha, quebra com a exceção do `Criar`.

Entidade cujo `Criar` devolve `Result<T>` também precisa de ajuda, porque o AutoFixture só usa sozinho um método de fábrica que devolva o próprio tipo. O `DespesaRecorrenteGenerator` é o modelo: um `ISpecimenBuilder` que pede cada argumento ao contexto, chama a `DespesaRecorrenteFactory` e devolve o `.Value`. Como os argumentos vêm do contexto, um `[Frozen]` ou um `fixture.Inject` no teste chega até a entidade.

### Peça o objeto ao AutoFixture, não o monte no teste

Antes de escrever `new` ou chamar um método de fábrica no corpo do teste, veja se o objeto pode vir como parâmetro. Se o AutoFixture ainda não sabe criá-lo e é provável que outros testes peçam o mesmo tipo, **crie a customização** em vez de montá-lo no teste. Num caso isolado, montar o objeto no teste ainda é melhor que uma customização que só ele usa. Os exemplos abaixo são tipos que vários testes pedem:

- `IdentidadeLancamentoCustomization` registra `(string id) => IdentidadeLancamento.Unica(id).Value`. Assim, o teste pede `IdentidadeLancamento identidade`, em vez de pedir uma `string idExterno` que só serviria para chamar o `Unica`.
- `OrigemTransacaoCustomization` registra a origem a partir de uma `ContaBancaria`. Com `[Frozen] ContaBancaria conta`, a `OrigemTransacao origem` do mesmo teste aponta para essa conta, sem `OrigemTransacao.DaConta(conta.Id)` no corpo.
- `[Frozen] Banco banco` e `[Frozen] SufixoConta sufixo` fazem duas `ContaBancaria` pedidas como parâmetro terem o mesmo banco e o mesmo sufixo, em vez de duas chamadas à `ContaBancariaFactory` no corpo do teste.

O teste monta o objeto à mão só quando os valores dele são o assunto do teste: o texto canônico de um hash conhecido, duas contas em bancos **diferentes**, a data e a descrição de um teste de sobreposição.

### Use o atributo mais simples que dê conta

`[DomainAutoData]` instancia e aplica todas as customizações do domínio a cada caso. Quando o teste não precisa de nenhuma delas, use o `[AutoData]` ou o `[InlineAutoData]` do `AutoFixture.Xunit2`, e o `[InlineData]` do xUnit quando nada for gerado.

O teste precisa do `DomainAutoData` quando pede value object com regra, id Vogen, SmartEnum, entidade, `decimal` com centavos ou `DateOnly`. O `DateOnly` entra na lista porque o AutoFixture puro gera um com dia e mês aleatórios, e o construtor estoura.

### Método interno se testa pela entrada pública

Não use `InternalsVisibleTo`. Um método `internal` está fechado porque só uma entrada pública pode chamá-lo, e é por ela que o teste passa, do mesmo jeito que a aplicação. O `Transacao.Criar` é interno, então a cópia dos campos e o id v7 são testados pela `LoteTransacoesFactory`.

## Quando o teste precisa de um valor específico

Cinco caminhos, do mais geral para o mais local:

1. **Customização**: a regra vale para todo teste. É o caso do `DiaVencimentoCartaoCustomization`, que traz qualquer inteiro para 1–28. Use quando o valor gerado por padrão seria inválido.
2. **AutoData privado da classe**: a regra vale para vários testes de uma classe só. A classe declara um atributo `private sealed` que herda do `DomainAutoDataAttribute` e recebe a configuração no construtor, como o `ImportacaoDoCartaoAutoDataAttribute` da `LoteTransacoesFactoryTests`, que monta a importação a partir do cartão, e não da conta. Prefira esse atributo a um método auxiliar que pede valores ao `Fixture` para montar o objeto no corpo do teste.
3. **Anotação no parâmetro**: o teste restringe só a si mesmo.
   - `[Range(1, 12)] int mes`, como em `MesReferenciaTests`, quando qualquer valor de uma faixa serve.
   - `[Frozen] Usuario esperado` congela a instância: todo objeto que o fixture montar depois e dependa de um `Usuario` recebe esse mesmo. É assim que o teste do cadastro sabe o que esperar no banco.

   **O `[Frozen]` é o jeito padrão de fixar um valor**, e o parâmetro congelado é lido pelo teste, no arrange ou no assert. Em `GeracaoDespesasDoMesServiceTests`, `[Frozen] DiaOcorrenciaRecorrente diaOcorrencia` faz a despesa recorrente que o fixture montar ocorrer nesse dia, e a data esperada sai de `diaOcorrencia.Value`.
4. **Mutação do objeto devolvido**: o teste parte de um objeto válido e muda só o que importa, com `with` nos records.

   ```csharp
   requisicao with { RendaBruta = -0.01m }
   ```

   Fica explícito que o caso é "uma requisição válida, exceto a renda". É o caminho dos casos inválidos e das bordas.
5. **`fixture.Inject` no corpo do teste**: só quando o `[Frozen]` não expressa o caso. Peça `Fixture fixture` como parâmetro e injete antes do `fixture.Create<T>()`. São dois casos:
   - o teste precisa de um valor específico, que o parâmetro não escolhe: `fixture.Inject(FormaPagamento.Credito)`;
   - o valor depende de algo montado no próprio teste, que nenhum parâmetro produz.

   No exemplo, o `[Frozen] IdUsuario` faz o cartão e a despesa recorrente saírem do mesmo usuário, sem montar um `Usuario`. Só a forma de pagamento precisa do `Inject`:

   ```csharp
   [Theory, DomainAutoData]
   public void Criar_Quando_FormaPagamentoCredito_Deve_...([Frozen] IdUsuario idUsuario, [Frozen] Cartao cartao, Fixture fixture)
   {
       fixture.Inject(FormaPagamento.Credito);
       DespesaRecorrente despesaRecorrente = fixture.Create<DespesaRecorrente>();
       ...
   ```

**Não contorne essas limitações.** Nada de atributo próprio que alimente o `[Frozen]` com `MemberData`, nem de parâmetro `[Frozen] T _` (ou `_1`) que só existe para congelar e nunca é lido: a assinatura do teste passa a esconder o que ele depende. Para variar poucos valores da mesma regra, use `[InlineDomainAutoData(...)]`.

Valor fixo direto no teste (`(TextoObrigatorio)"INSS"`) só quando o próprio valor é o assunto, como num teste de soma com o resultado calculado à mão.

## Exceção esperada

Use `ThrowExactly<T>()`, e não `Throw<T>()`, que também passa com uma exceção derivada de `T`: `registrar.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("lidas")`.

## Nome do teste

`Metodo_Quando_Cenario_Deve_Resultado`, como no `CLAUDE.md`. Quando não há cenário, porque o comportamento vale sempre, o `Quando` sai em vez de ganhar um cenário vazio: `Criar_Deve_GerarIdVersao7ParaCadaTransacao`, e não `Criar_Quando_Chamado_Deve_GerarIdVersao7ParaCadaTransacao`.

## Customizações realistas (só na integração)

`tests/ControleFinanceiro.Tests.Shared/AutoFixture/Customizations/Realistas/` troca os valores do AutoFixture por valores com cara de dado real: nome completo em pt-BR (com acento), renda entre R$ 1.500 e R$ 30.000 com centavos, descrição de desconto de contracheque. Elas:

- são **opt-in**: o `DomainFixture` não as aplica, porque os testes de domínio testam melhor as bordas com os decimais do `StrictDecimalCustomization`;
- servem aos testes de integração, em que o valor atravessa JSON e Postgres (acento no caminho UTF-8, centavos no `numeric`) e a falha precisa ser legível;
- usam o Bogus só onde o AutoFixture e o `Random` não dão conta: o nome de pessoa;
- têm seed fixa, para um teste que falhou gerar os mesmos dados de novo;
- geram só nomes, valores e descrições, nunca CPF, conta ou número de cartão completo.

## Testes de integração da API

`tests/ControleFinanceiro.WebApi.Tests` sobe a API em memória (`WebApplicationFactory`) contra um Postgres do Testcontainers. **Precisa do Docker rodando**: sem ele, os testes falham, não são pulados.

- **Pastas espelhadas.** O teste de `src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/` mora em `tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/`. O `pre-push` usa esse espelho para rodar só os testes afetados (ver [harness.md](harness.md#testes-de-integração-afetados)).
- **`[ApiAutoData]`**, ou `[InlineApiAutoData(...)]`, é o `DomainFixture` com as customizações realistas e as de request.
- **Customização de request.** Cada endpoint tem uma customização na própria pasta, como `CadastrarUsuarioRequestCustomization`, que monta o DTO a partir da entidade do fixture: `fixture.Register((Usuario u) => new CadastrarUsuarioRequest { ... })`. Todo teste que pede o request recebe um válido, e nenhum teste repete o mapeamento.
- **Um container para a suíte.** A `ApiComPostgresFixture` fica na collection `ApiCollection`. Cada classe chama `ResetarBancoAsync()` no `InitializeAsync`, antes de cada teste, para o que um teste que falhou gravou ficar no banco para depuração.
- **Leitura de volta pelo banco**, com `LerTodosAsync(db => db.Contas)` ou `ConsultarAsync`, num escopo novo e com `AsNoTracking`. O teste confere o que foi gravado, não o que a requisição tinha em memória.
- **O que se repete entre os testes fica num lugar só**, em `Infraestrutura/`:
  - `ApiComPostgresFixture` cadastra o usuário (`CadastrarUsuarioAsync`) e lê as tabelas de volta;
  - `CamposComErroAsync` lê os campos com erro do `ProblemDetails` da resposta;
  - `ArquivoFormulario.ComTamanho` monta o campo `arquivo` para os testes do validator.

  Nos testes dos leitores, o `ArquivoFixture` faz o mesmo papel: lê o arquivo fictício de `Fixtures/` e monta as variações dele no layout de bytes do banco.

### O que vai para a integração

Cada teste de integração sobe uma requisição HTTP e vai ao Postgres. Se cada borda de regra virar um teste de endpoint, a suíte fica lenta à medida que os endpoints chegam. Por isso a integração cobre **só o que depende da API e do banco juntos**:

- a requisição válida gravada e lida de volta;
- cada status que o endpoint decide (404, 409...), com o campo do erro;
- **um** 400 com todos os campos inválidos, que prova que o validator está ligado e que cada erro sai no campo certo;
- o que é risco do mapeamento do EF, visto pela API: entidade nova num agregado carregado, conversão de tipo.

Nada de teste que grava direto no banco, com SQL ou fora da API: a API é a única porta de entrada dos dados, então índice, `CHECK` e cascade não ganham teste próprio.

O resto vai para a unidade:

| Regra | Onde |
|---|---|
| Borda de value object (limite do dia, trim, sufixo malformado) | `ControleFinanceiro.Domain.Tests`, no teste do value object |
| Invariante do agregado, ou regra de uma factory de domínio (sufixo repetido no mesmo banco) | `ControleFinanceiro.Domain.Tests`, no teste do agregado ou da factory |
| Regra que o validator acrescenta (código de SmartEnum, id vazio) | `<Acao>ValidatorTests`, na pasta do teste do endpoint, sem `ApiCollection`, com `TestValidate` |

O `AdicionarCartao` é o modelo: cinco casos de integração no `AdicionarCartaoEndpointTests` e as regras do banco e do id no `AdicionarCartaoValidatorTests`.

```bash
dotnet test tests/ControleFinanceiro.WebApi.Tests
dotnet test tests/ControleFinanceiro.WebApi.Tests --filter "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar."
```
