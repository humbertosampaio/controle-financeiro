# Endpoints

- **Pasta por área e ação**: `Endpoints/<Area>/<Acao>/`, com o endpoint, o `<Acao>Request`, o `<Acao>Response` e o validator. O endpoint FastEndpoints é descoberto sozinho; o repositório que ele usa é registrado no `DependencyInjection.cs` da Persistence.
- **Teste de integração na pasta espelhada**, `tests/ControleFinanceiro.WebApi.Tests/<Area>/<Acao>/`, com `[ApiAutoData]` e lendo de volta do banco. Endpoint novo ou alterado tem o seu; fora do espelho, o `pre-push` não roda o teste quando o endpoint muda. O que vai para a integração e o que fica na unidade está no [guia de testes](../../docs/testes-com-autofixture.md#o-que-vai-para-a-integração).
- **Cada `Erro` tem o seu status, num `switch` só com `default` que lança**, como o `EnviarErroAsync` do `AdicionarCartaoEndpoint`. Operação que passa a devolver um tipo de `Erro` sem `case` nesse `switch` faz a falha prevista sair como 500. Compare o erro pelo tipo, nunca pelo `Codigo` ou pela `Descricao` (os tipos de erro estão nos [padrões de domínio, §5](../../docs/padroes-de-dominio.md#5-falhas)).
- **O validator repete a regra do value object chamando o `Criar` dele**, pelo `MustBeValueObject`, para responder 400 sem duplicar a regra. A mensagem de falha vem do `.Error`.
- **Regra de negócio não mora no endpoint**: ele carrega os agregados pelos repositórios, chama o domínio e grava.
