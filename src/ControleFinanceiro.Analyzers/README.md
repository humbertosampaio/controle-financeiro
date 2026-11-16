# Analisadores das convenções

Os analisadores deste projeto conferem no build as convenções de C# que o Roslyn não confere. O `Directory.Build.props` da raiz os liga em todo projeto da solução, menos neste e no dos testes deles, e o `TreatWarningsAsErrors` faz de cada diagnóstico um erro. O código gerado fica de fora: as Migrations (`generated_code = true` no `.editorconfig`) e o que o Vogen e o FastEndpoints geram.

Este catálogo é a fonte do texto das regras que têm gate: o `src/CLAUDE.md` e o `revisor-convencoes` só apontam para cá. Os testes ficam em `tests/ControleFinanceiro.Analyzers.Tests`, com um caso que aponta, um que não aponta e um por exceção.

## CF0001

**Block body em método multilinha.** Método, operador, conversão ou função local cujo corpo passa de uma linha usa block body, inclusive uma instrução única quebrada em várias linhas. O `=>` serve quando a expressão ocupa uma linha só, na linha da assinatura ou logo abaixo.

- Fora: lambdas e propriedades. O construtor usa sempre block body, mesmo de uma linha, pelo IDE0021.
- Por quê: o `=>` esconde o começo e o fim do corpo quando a expressão quebra, e o IDE0022 do Roslyn só sabe exigir ou proibir o `=>`, sem olhar o número de linhas.
