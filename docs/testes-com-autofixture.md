# Testes com AutoFixture

O AutoFixture existe aqui para uma coisa: **dar ao teste um objeto válido sem que ele precise saber os valores de dentro.** Um teste de `AdicionarCartao` precisa de um `Usuario`, mas não liga para o nome nem para a renda. Se o teste montasse o usuário à mão, um `"Maria"` e um `5000m` apareceriam no código como se importassem para o resultado.

Por isso, a regra é: **o que o teste não verifica, o AutoFixture gera; o que ele verifica, o teste declara.**

## Como um objeto válido nasce

Os value objects só existem válidos (`SufixoCartao.Criar` falha para algo que não sejam 4 dígitos, `DiaVencimentoCartao.Criar` falha fora de 1 a 28). O AutoFixture, sozinho, não conhece essas regras: geraria `"a3f9…"` para o sufixo e `173` para o dia. As customizações em `tests/ControleFinanceiro.Tests.Shared/AutoFixture/Customizations/Domain/Shared/` ensinam a regra a ele, uma por value object, e o `DomainFixture` reúne todas.

Value object novo com regra pede customização nova no `DomainFixture`. Sem ela, qualquer teste que peça o tipo, ou uma entidade que o contenha, quebra com a exceção do `Criar`.

## Quando o teste precisa de um valor específico

Três caminhos, do mais geral para o mais local:

1. **Customização**: a regra vale para todo teste. É o caso do `DiaVencimentoCartaoCustomization`, que traz qualquer inteiro para 1–28. Use quando o valor gerado por padrão seria inválido.
2. **Anotação no parâmetro**: o teste restringe só a si mesmo.
   - `[Range(1, 12)] int mes`, como em `MesReferenciaTests`, quando qualquer valor de uma faixa serve.
   - `[Frozen] Usuario esperado` congela a instância: todo objeto que o fixture montar depois e dependa de um `Usuario` recebe esse mesmo. É assim que o teste do cadastro sabe o que esperar no banco.
3. **Mutação do objeto devolvido**: o teste parte de um objeto válido e muda só o que importa, com `with` nos records.

   ```csharp
   requisicao with { RendaBruta = -0.01m }
   ```

   Fica explícito que o caso é "uma requisição válida, exceto a renda". É o caminho dos casos inválidos e das bordas.

Valor fixo direto no teste (`(TextoObrigatorio)"INSS"`) só quando o próprio valor é o assunto, como num teste de soma com o resultado calculado à mão.

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
- **Leitura de volta pelo banco**, com `ConsultarAsync` num escopo novo e `AsNoTracking`. O teste confere o que foi gravado, não o que a requisição tinha em memória.

```bash
dotnet test tests/ControleFinanceiro.WebApi.Tests
dotnet test tests/ControleFinanceiro.WebApi.Tests --filter "FullyQualifiedName~ControleFinanceiro.WebApi.Tests.Usuarios.Cadastrar."
```
