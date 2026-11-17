# Convenções de C#

Valem para todo o código de `src/` e, salvo onde está escrito "só em `src/`", também para o de `tests/`. Código novo segue as regras, e o existente só muda quando for tocado.

O build confere as regras com gate, e o texto delas fica no [catálogo dos analisadores](ControleFinanceiro.Analyzers/README.md):

- CF0001: block body em método multilinha;
- CF0002: ternário sem ramo multilinha;
- CF0003: sem construtor primário em classe;
- CF0004: extensões no bloco `extension`;
- CF0005: `.ToArray()` em vez de spread;
- CF0006: duas conversões seguidas, só em `src/`;
- CF0007: argumentos irmãos no mesmo grau;
- CF0009: nome de teste;
- CF0009b: sufixo `Result`;
- CF0011: argumento literal nomeado;
- CF0012: `is T x`, e não `is { } x`, para desembrulhar nullable;
- CF0013: sem `.Value` onde o operador implícito já basta;
- IDE0021: block body em construtor;
- `dotnet format style`: ordem dos usings, com os `System.*` entre os demais;
- só em `src/`, IDE0005 (using sem uso) e CA1851 (enumeração múltipla no mesmo método).

O resto o `revisor-convencoes` cobra.

## Nomes

- **Sem preposição (preferência, sem gate).** Identificador de tipo, membro, parâmetro ou variável junta os substantivos direto, sem "do", "da", "de", "dos" ou "das": `PagamentoFaturaAnterior`, `importacoesFaturaMes`. O cenário e o resultado no nome de teste ficam de fora, porque são uma frase (`Quando_SemCategoria_Deve_...`). "Em" e "Por" ficam quando dizem o papel do argumento e tirá-los deixaria o nome ambíguo: `ListarFaturaEmAsync(mes)`, `ListarPagasPorAsync(ids)`, e não `ListarMesVencimentoFaturaAsync`, que parece listar meses.
- **O nome diz o que guarda.** Quando o nome de uma variável ou parâmetro qualifica um objeto, o tipo vai no nome: `cartaoOutroUsuario`, e não `outroUsuario`, que parece um `Usuario`. Método e tipo levam o conceito inteiro, e não só o papel: `CandidatoPagamentoFaturaUnico`, e não `CandidatoUnico`. O "só" vira "Apenas" (`ApenasLinhasFinal`), porque `So` se confunde com o inglês.
- **Plural sozinho é coleção.** O membro que conta leva `Quantidade`: `Parcela.QuantidadeParcelasAnteriores`, e não `Anteriores`. Fica de fora o `int`, que já diz que conta (`MesReferencia.Anterior(int meses)`).
- **O sufixo diz a camada.** Serviço de domínio termina em `Service` (`PagamentoFaturaService`) e o que ele devolve não leva sufixo (`CandidatoPagamentoFatura`). Serviço da Application termina em `AppService` (`ImportacaoExtratoAppService`), e o que ele devolve, em `Dto` (`ExtratoImportadoDto`); o campo e o parâmetro que o recebem por DI levam o mesmo sufixo (`_importacaoExtratoAppService`). A WebApi usa `Request` e `Response`, e a fronteira com o modelo, `ParaModeloService` e `ParaModelo`. O `NomesCamadaTests` confere os sufixos dos serviços, e o CF0009b, o `Result`.

## Forma do código

- **Cadeia fluent que deixaria a linha longa quebra uma chamada por linha**, cada uma começando pelo `.` e um nível abaixo da origem (`pendentes` / `.Select(...)` / `.ToArray()`). Cadeia curta fica numa linha.
- **O literal único cujo papel o nome do método não diz vai nomeado** (`Pagar(pago: true)`). O CF0011 deixa todo argumento único de fora, e o `AddDays(1)` fica como está.
- **Parâmetro genérico, retorno específico**, sem abstração vazada: o parâmetro genérico aceita mais tipos, e o retorno específico dá mais a quem chama. Quem serve às duas origens recebe a `Transacao`; quem só serve à conta, a `TransacaoConta`. Na coleção, o parâmetro público é o mais genérico que o algoritmo aceita (`IEnumerable<T>` só com LINQ, `IReadOnlyCollection<T>` com `Count`, `IReadOnlyList<T>` com índice), e ele devolve `IReadOnlyList<T>` ou `IReadOnlyCollection<T>`. O privado recebe o tipo de quem chama e devolve o mais específico que monta: `T[]` se quem chama não a altera, `List<T>` se um método dela (`Add`, `Find`) lhe serve.
- **Dentro da própria classe, o campo privado** (`_cartoes.Exists(...)`), e não a propriedade exposta.
- **Comentário que repete o código, ou que o diff desatualizou**, sai.
- Só em `src/`: **chamada que não depende do elemento da lambda ou do laço é feita antes dela**, para não se repetir a cada iteração (o `IdBeneficiario.From(guid)` antes do `FirstOrDefault`). Vale para método e conversão; ler na lambda uma propriedade ou um índice já calculados fica. Nos testes, a conversão pode ficar dentro da lambda.
- Só em `src/`: **sequência lazy lida mais de uma vez é materializada antes** com `.ToArray()`. O CA1851 pega a releitura no mesmo método; conte também as leituras nos métodos que a recebem. Lida uma vez, pode ficar lazy.

## Value objects, nulos e conversões

- **Cast ou `Criar`.** O `explicit operator` do primitivo ([padrões de domínio, §7](../docs/padroes-de-dominio.md#7-value-objects-e-ids)) serve onde o valor já é sabidamente válido (teste, generator, conversão EF) e onde o `Criar` nunca falha; com entrada do usuário ou do arquivo, use o `Criar` e trate o `Result`.
- **Value object se compara, ordena e agrupa inteiro** (`OrderBy(p => p.Key)`, `c.Sufixo == sufixo`), sem abrir o `.Value`: quem decide a comparação é o `GetEqualityComponents`. Para comparar com um primitivo, converta o primitivo para o value object (`b.Id == IdBeneficiario.From(guid)`), e não o contrário.
- **O `Maybe<T>` tem conversão implícita de `T?`**: devolva o valor direto, sem `Maybe.From`.

## Texto e SmartEnum

- **Texto fixo num lugar só.** A mensagem fixa que o domínio define chega às outras camadas pelo `Result` ou pelo `Erro` que a carrega, e nenhuma camada a redefine nem a guarda numa constante para repetir: o `Fatura.ConferirPagamentoAnterior` falha com o próprio texto, a API lê o `.Error`, e a saída para o modelo o recebe como `DocumentoMascarado`. Só tem texto próprio a saída cuja mensagem de domínio traz valores, como a da soma do período. Nos testes, o assert copia o texto literal, para não conferir o resultado com o mesmo código que o produziu.
- **Código e nome de SmartEnum vêm do SmartEnum.** O texto que cita um SmartEnum usa o `.Value` para o código (`$"O layout {LayoutArquivoImportacao.C6CartaoCsv.Value} pede..."`) e o próprio SmartEnum, sem `.Value`, para o nome, e nunca o literal, inclusive no comentário XML, que usa `<see cref>`. Ficam de fora o atributo, que exige constante, e o assert de teste.

## Injeção de dependência

**Transiente, depois scoped, depois singleton.** Serviço sem estado é transiente, mesmo que dependa de algo scoped. Scoped só quando a instância guarda estado de uma requisição ou chamada (o `DbContext`); singleton só para instância compartilhada sem estado mutável (o `TimeProvider.System`). O tempo de vida decide quanto dura a instância, e não o valor que ela devolve: o `IUsuarioAtual` que lê a configuração ou a rota a cada acesso é transiente.

## Orquestração com `Result` (diretriz, não regra)

Prefira uma cadeia plana de métodos ao encadear etapas que devolvem `Result` ou `Maybe`, mas a legibilidade decide: quando a forma plana ficar mais difícil de ler que um `Bind` aninhado curto, um `if (result.IsFailure) return ...` ou um laço comum, use a alternativa. A forma preferida:

- as etapas se encadeiam com `Bind`, `Map`, `Ensure`, `MapError` e `Match` no mesmo nível, sem `Bind` dentro do lambda de outro e sem query syntax;
- cada etapa é um método privado que recebe só o que a anterior produziu, como method group quando a assinatura bate (`.Bind(LerContaEPeriodo)`);
- quando a etapa seguinte precisa de mais de um valor, a anterior devolve uma tupla nomeada com alias no topo do arquivo, com namespace completo (`using ContaEPeriodo = (ControleFinanceiro.Domain.Contas.SufixoConta Sufixo, ...);`), em vez de record auxiliar ou closure;
- o método que monta a tupla pode aninhar um nível: `LerSufixoConta(c).Bind(sufixo => LerPeriodo(c).Map(periodo => (sufixo, periodo)))`. O `Result.Combine` só serve quando todas as falhas devem aparecer juntas;
- laço sobre I/O pode ser imperativo, dentro de um método que devolve `Result`.

Exemplo: `LeitorExtratoContaC6.LerAsync`.
