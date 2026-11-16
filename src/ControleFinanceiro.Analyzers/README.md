# Analisadores das convenções

Os analisadores deste projeto conferem no build as convenções de C# que o Roslyn não confere. O `Directory.Build.props` da raiz os liga em todo projeto da solução, menos neste e no dos testes deles, e o `TreatWarningsAsErrors` faz de cada diagnóstico um erro. O código gerado fica de fora: as Migrations (`generated_code = true` no `.editorconfig`) e o que o Vogen e o FastEndpoints geram.

Este catálogo é a fonte do texto das regras que têm gate: o `src/CLAUDE.md` e o `revisor-convencoes` só apontam para cá. Os testes ficam em `tests/ControleFinanceiro.Analyzers.Tests`, com um caso que aponta, um que não aponta e um por exceção.

## CF0001

**Block body em método multilinha.** Método, operador, conversão ou função local cujo corpo passa de uma linha usa block body, inclusive uma instrução única quebrada em várias linhas. O `=>` serve quando a expressão ocupa uma linha só, na linha da assinatura ou logo abaixo.

- Fora: lambdas e propriedades. O construtor usa sempre block body, mesmo de uma linha, pelo IDE0021.
- Por quê: o `=>` esconde o começo e o fim do corpo quando a expressão quebra, e o IDE0022 do Roslyn só sabe exigir ou proibir o `=>`, sem olhar o número de linhas.

## CF0002

**Sem ternário com ramo de mais de uma linha.** Quando um ramo do `? :` quebra em várias linhas, use `if`. Ramos de uma linha cada podem ficar no ternário, mesmo com a expressão inteira em três linhas.

- Por quê: com um ramo quebrado, o `?` e o `:` se perdem no meio das linhas, e o leitor não vê onde um ramo acaba e o outro começa.

## CF0003

**Sem construtor primário em classe nem em struct.** A dependência vai para um campo `private readonly` (`_fixture`), atribuído num construtor comum; vale também para a classe de teste que recebe a fixture e para o value object que repassa o valor à base, como o `Reais`.

- Fora: records posicionais, cujo construtor primário declara as propriedades.
- Por quê: o parâmetro do construtor primário fica visível na classe inteira, sem `readonly` e sem o `_` que diz que é campo. O IDE0290 do Roslyn só sugere o construtor primário, e não aponta o que já existe.

## CF0004

**Extensões no bloco `extension` do C# 14**, e não com `this` no primeiro parâmetro. O helper privado que só as extensões usam fica no mesmo bloco.

- Vale também para o `ServiceDefaults/Extensions.cs`, que veio do template do Aspire: uma exceção a menos para lembrar.
- Por quê: o bloco junta as extensões do mesmo tipo e declara o receptor uma vez só, e o C# 14 permite nele propriedades de extensão, que o `this` não permite.

## CF0005

**`.ToArray()` ou `.ToList()` em vez de spread** para materializar uma sequência: `numeros.ToArray()`, e não `[.. numeros]`.

- Fora: a expressão de coleção que junta mais de um elemento, como `[.. a, .. b]` ou `[.. a, novo]`.
- Por quê: o spread sozinho só copia, e o `.ToArray()` diz o tipo da cópia no nome. O IDE0305, que pediria o spread, está desligado no `.editorconfig`.

## CF0006

**Duas conversões explícitas seguidas pedem uma forma melhor.** Um `(X)(string)y` costuma revelar o `explicit operator` direto de `Y` para `X` que falta, como o de `TextoObrigatorio` para `DocumentoMascarado`: crie-o.

- Só em `src/`: vem desligado e o `.editorconfig` o liga na seção `[src/**/*.cs]`. Nos testes, as duas conversões ficam quando o assunto é a passagem pelo tipo do meio, como num teste que monta um `TextoObrigatorio` de um literal.
- Por quê: a conversão pelo tipo do meio repete em cada lugar o caminho que o operador direto diria uma vez.

## CF0009

**Nome de teste no formato `Metodo_Quando_Cenario_Deve_Resultado`**, ou `Metodo_Deve_Resultado` quando não há cenário. Vale para todo método com `[Fact]`, `[Theory]` ou um atributo que derive deles, e confere o nome com `^[A-Z][A-Za-z0-9]*(_Quando_[A-Za-z0-9]+)?_Deve_[A-Za-z0-9]+$`.

- O resto da regra (um comportamento por teste, sem relógio real) está no [guia de testes](../../docs/testes-com-autofixture.md#nome-do-teste).
- Por quê: o formato fixo deixa ler o método, o cenário e o resultado na lista de testes, sem abrir o código.
