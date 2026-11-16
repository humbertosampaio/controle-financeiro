# Persistência

O mapeamento segue os [padrões de domínio, §8](../../docs/padroes-de-dominio.md#8-persistência-dos-limites): `IEntityTypeConfiguration` na pasta do agregado, FK entre agregados sem navegação e o filtro `FiltroUsuario` montado sozinho.

- **Agregado sempre carregado inteiro** (#36): cada navegação de coleção é `AutoInclude()` na configuração, como `DescontosSalariais` no `UsuarioConfiguration`. O split query é global (`UseQuerySplittingBehavior` no `DependencyInjection.cs`): nada de `AsSplitQuery()` ou `AsSingleQuery()` sem justificativa no código, nem `IgnoreAutoIncludes()` numa consulta que devolve o agregado para ser alterado.
- **Conversão de value object.** Inline na configuração, com lambda (`HasConversion(s => s.Value, v => (SufixoConta)v)`), para o value object que tende a não ser usado em outro lugar; `ValueConverter` reaproveitável em `ValueConverters/ValueObjects/` para o usado por vários agregados (os de `Shared`, como `TextoObrigatorio` e `Reais`). Sem `explicit operator` no value object, crie um, em vez de `.Criar(v).Value` na lambda.
- **Id Vogen novo** precisa do conversor no `VogenEfCoreConverters.cs`. **Entidade nova** precisa de `IEntityTypeConfiguration`, `DbSet` e migration; o repositório novo é registrado no `DependencyInjection.cs`.
- **Migration que apaga ou renomeia coluna ou tabela** (`DropColumn`, `DropTable`, `RenameColumn`, mudança de tipo com perda) pede confirmação antes de ser gerada (inviolável 8), e a decisão fica registrada na issue, porque o banco local tem dados reais. O `Down()` é coerente com o `Up()`.
- **Mudou o modelo, gere a migration.** `dotnet ef migrations has-pending-model-changes --project src/ControleFinanceiro.Infrastructure.Persistence` sai com código 1 quando falta uma; o CI confere.
