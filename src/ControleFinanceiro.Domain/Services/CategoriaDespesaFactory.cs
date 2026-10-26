using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using AtribuicaoAlias = (ControleFinanceiro.Domain.Categorias.CategoriaDespesa Destino, ControleFinanceiro.Domain.Shared.ValueObjects.Alias Alias);

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria e renomeia as categorias do usuário e atribui aliases a elas, garantindo um nome por categoria (sem contar
/// caixa, acentos e espaços repetidos) e uma categoria por alias em cada usuário. O índice único dos aliases é a rede
/// para duas requisições ao mesmo tempo.
/// </summary>
public static class CategoriaDespesaFactory
{
    /// <summary>A categoria <see cref="CategoriaDespesa.NomeOutros"/>, criada junto com o usuário.</summary>
    public static CategoriaDespesa CriarOutros(IdUsuario idUsuario) =>
        CategoriaDespesa.Criar(idUsuario, CategoriaDespesa.NomeOutros);

    /// <summary>
    /// Aplica o lote inteiro ou nada: cria as categorias novas e adiciona os aliases, às novas e às existentes. O alias
    /// que a categoria de destino já tem não muda nada.
    /// </summary>
    /// <remarks>
    /// Só as categorias novas precisam ser adicionadas ao repositório; as existentes que ganharam alias já estão nele.
    /// </remarks>
    /// <param name="categoriasDoUsuario">Todas as categorias do usuário, e só as dele.</param>
    /// <returns>As categorias novas, na ordem do lote.</returns>
    /// <exception cref="ArgumentException">
    /// Quando a lista traz uma categoria de outro usuário, ou um item aponta para uma categoria fora da lista.
    /// </exception>
    public static Result<IReadOnlyList<CategoriaDespesa>, Erro> Resolver(
        IdUsuario idUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        IEnumerable<IItemLoteCategorias> itens)
    {
        GarantirDoUsuario(idUsuario, categoriasDoUsuario);

        List<CategoriaDespesa> novas = [];
        List<AtribuicaoAlias> atribuicoes = [];

        foreach (IItemLoteCategorias item in itens)
        {
            UnitResult<Erro> aplicado = item switch
            {
                CategoriaNova nova => CriarNova(idUsuario, categoriasDoUsuario, nova, novas, atribuicoes),
                AliasCategoriaExistente existente => AtribuirAExistente(categoriasDoUsuario, existente, atribuicoes),
                _ => throw new ArgumentException($"Item de lote desconhecido: {item.GetType().Name}.", nameof(itens)),
            };

            if (aplicado.IsFailure)
                return aplicado.Error;
        }

        if (OutroDono(categoriasDoUsuario, atribuicoes) is CategoriaDespesa dono)
            return new ErroAliasCategoriaJaAtribuido(dono.Id);

        foreach ((CategoriaDespesa destino, Alias alias) in atribuicoes)
            destino.AdicionarAlias(alias);

        return novas;
    }

    /// <summary>
    /// Renomear a categoria para o próprio nome, com outra caixa ou acentuação, é permitido. A
    /// <see cref="CategoriaDespesa.NomeOutros"/> não é renomeada, regra que o próprio agregado guarda.
    /// </summary>
    /// <param name="categoriasDoUsuario">Todas as categorias do dono da <paramref name="categoria"/>, e só as dele.</param>
    /// <exception cref="ArgumentException">Quando a lista traz uma categoria de outro usuário.</exception>
    public static UnitResult<Erro> Renomear(
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        CategoriaDespesa categoria,
        TextoObrigatorio nome)
    {
        GarantirDoUsuario(categoria.IdUsuario, categoriasDoUsuario);

        if (categoriasDoUsuario.FirstOrDefault(c => c != categoria && c.PossuiNome(nome)) is CategoriaDespesa mesmoNome)
            return new ErroCategoriaJaCadastrada(mesmoNome.Id);

        return categoria.Renomear(nome);
    }

    /// <summary>
    /// Cria a categoria do item e a guarda em <paramref name="novas"/>, com o alias dela em
    /// <paramref name="atribuicoes"/>, para os itens seguintes e a conferência dos aliases a enxergarem.
    /// </summary>
    private static UnitResult<Erro> CriarNova(
        IdUsuario idUsuario,
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        CategoriaNova nova,
        List<CategoriaDespesa> novas,
        List<AtribuicaoAlias> atribuicoes)
    {
        if (categoriasDoUsuario.Concat(novas).FirstOrDefault(c => c.PossuiNome(nova.Nome)) is CategoriaDespesa mesmoNome)
            return new ErroCategoriaJaCadastrada(mesmoNome.Id);

        CategoriaDespesa criada = CategoriaDespesa.Criar(idUsuario, nova.Nome);
        novas.Add(criada);
        if (nova.Alias is not null)
            atribuicoes.Add((criada, nova.Alias));

        return UnitResult.Success<Erro>();
    }

    /// <summary>Guarda o alias do item em <paramref name="atribuicoes"/>; nenhum item de categoria existente falha aqui.</summary>
    /// <exception cref="ArgumentException">Quando a categoria do item não está na lista.</exception>
    private static UnitResult<Erro> AtribuirAExistente(
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        AliasCategoriaExistente existente,
        List<AtribuicaoAlias> atribuicoes)
    {
        if (!categoriasDoUsuario.Contains(existente.Categoria))
            throw new ArgumentException("O lote aponta para uma categoria fora da lista.", "itens");

        atribuicoes.Add((existente.Categoria, existente.Alias));
        return UnitResult.Success<Erro>();
    }

    /// <summary>
    /// A categoria que já tem o alias de uma atribuição, ou que o recebe numa atribuição anterior do lote, quando não é
    /// o destino dela.
    /// </summary>
    private static CategoriaDespesa? OutroDono(
        IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario,
        List<AtribuicaoAlias> atribuicoes)
    {
        Dictionary<Alias, CategoriaDespesa> donos = [];

        foreach ((CategoriaDespesa destino, Alias alias) in atribuicoes)
        {
            CategoriaDespesa? dono = donos.GetValueOrDefault(alias)
                ?? categoriasDoUsuario.FirstOrDefault(c => c.PossuiAlias(alias));

            if (dono is not null && dono != destino)
                return dono;

            donos[alias] = destino;
        }

        return null;
    }

    private static void GarantirDoUsuario(IdUsuario idUsuario, IReadOnlyCollection<CategoriaDespesa> categoriasDoUsuario)
    {
        if (categoriasDoUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma categoria de outro usuário.", nameof(categoriasDoUsuario));
    }
}
