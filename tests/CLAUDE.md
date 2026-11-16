@../src/CLAUDE.md

# Testes

As convenções de C# de `src/CLAUDE.md`, importado acima, valem também aqui, menos as marcadas "só em `src/`". **Leia o [docs/testes-com-autofixture.md](../docs/testes-com-autofixture.md) inteiro antes do primeiro teste**: ele é a fonte do nome do teste, do `[Frozen]`, das customizações, do que vai para a integração e do que cobrir.

## Onde mora cada teste

| Alvo | Projeto | Padrão |
|---|---|---|
| Agregado, value object, serviço de domínio | `tests/ControleFinanceiro.Domain.Tests/<Area>/` | `[Theory, DomainAutoData]`, ou `[AutoData]` quando nenhuma customização do domínio é necessária |
| Endpoint da API | `tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/`, espelhando `src/ControleFinanceiro.WebApi/Endpoints/<Area>/<Acao>/` | `[Collection(ApiCollection.Nome)]`, `ResetarBancoAsync()` no `InitializeAsync`, `[Theory, ApiAutoData]`, leitura de volta com `ConsultarAsync` |
| Validator do endpoint (regra que não é de value object) | mesma pasta do teste do endpoint, `<Acao>ValidatorTests` | sem `ApiCollection`, `[Theory, ApiAutoData]`, `TestValidate` |
| Servidor MCP | `tests/ControleFinanceiro.Mcp.Tests/Servidor/` | `[Collection(ServidorMcpCollection.Nome)]`, processo real pelo `ClienteMcp` da `ServidorMcpFixture`, `[Fact]` quando não há entrada gerada |
| Tipo de saída para o modelo e fronteira da saída | `tests/ControleFinanceiro.SaidaModelo.Tests/Tipos/` para o payload, `Arquitetura/` para as regras da fronteira e as dependências entre camadas | sem Docker, `[Theory, DomainAutoData]`, `.ParaModelo().Serializar()` comparado como JSON; regra nova vista falhando antes |
| Customização do AutoFixture | `tests/ControleFinanceiro.Tests.Shared/AutoFixture/` | uma por value object com regra, e uma para o tipo que vários testes montariam à mão |
| Customização de request | na pasta do teste do endpoint, registrada no `ApiFixture` | `fixture.Register((Entidade e) => new XRequest { ... })` |
| Leitor de extrato ou fatura | `tests/ControleFinanceiro.Infrastructure.Importacao.Tests/` | arquivo fictício em `Fixtures/`, lido pelo `ArquivoFixture` |

Leia dois testes vizinhos antes do primeiro. `Result` se verifica com `result.Should().Succeed()` / `.Fail()` (CSharpFunctionalExtensions.FluentAssertions).

## Docker

A `WebApi.Tests` e a `Mcp.Tests` sobem um Postgres do Testcontainers: sem o Docker, elas falham, e não são puladas. Com ele parado, diga isso em vez de reportar os testes como falhos.

## Dados de teste

- Fixture é inventada: mesmo cabeçalho, separador, formatos e encoding do arquivo real, com nomes, valores e números de conta fictícios. Nunca copie de `dados/` (inviolável 1 do `CLAUDE.md` da raiz).
- CPF e CNPJ de teste são sempre aleatórios, com verificadores válidos, ou um dos de exemplo já usados nos testes. Nenhuma varredura confere documento em fixture: esse cuidado é o único controle.
