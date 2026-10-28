using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using AtribuicaoAlias = (ControleFinanceiro.Domain.Beneficiarios.Beneficiario Destino, ControleFinanceiro.Domain.Shared.ValueObjects.Alias Alias);

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria os beneficiários do usuário e atribui aliases a eles, garantindo que cada alias aponte para um beneficiário só
/// em cada usuário.
/// </summary>
/// <remarks>
/// O nome não é único, porque a busca casa só com os aliases. Duas pessoas podem ter o mesmo nome no mundo real, e cabe
/// ao usuário, se quiser, dar a elas nomes diferentes no sistema. O índice único dos aliases é a rede para duas
/// requisições ao mesmo tempo.
/// </remarks>
public static class BeneficiarioFactory
{
    /// <summary>
    /// Aplica o lote inteiro ou nada: cria os beneficiários novos e adiciona os aliases, aos novos e aos existentes. O
    /// alias que o beneficiário de destino já tem não muda nada.
    /// </summary>
    /// <remarks>
    /// Só os beneficiários novos precisam ser adicionados ao repositório; os existentes que ganharam alias já estão nele.
    /// </remarks>
    /// <param name="beneficiariosDoUsuario">Todos os beneficiários do usuário, e só os dele.</param>
    /// <returns>Os beneficiários novos, na ordem do lote.</returns>
    /// <exception cref="ArgumentException">
    /// Quando a lista traz um beneficiário de outro usuário, ou um item aponta para um beneficiário fora da lista.
    /// </exception>
    public static Result<IReadOnlyList<Beneficiario>, Erro> Resolver(
        IdUsuario idUsuario,
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        IEnumerable<IItemLoteBeneficiarios> itens)
    {
        GarantirDoUsuario(idUsuario, beneficiariosDoUsuario);

        List<Beneficiario> novos = [];
        List<AtribuicaoAlias> atribuicoes = [];

        foreach (IItemLoteBeneficiarios item in itens)
        {
            AtribuicaoAlias? atribuicao = item switch
            {
                BeneficiarioNovo novo => CriarNovo(idUsuario, novo, novos),
                AliasBeneficiarioExistente existente when !beneficiariosDoUsuario.Contains(existente.Beneficiario) =>
                    throw new ArgumentException("O lote aponta para um beneficiário fora da lista.", nameof(itens)),
                AliasBeneficiarioExistente existente => (existente.Beneficiario, existente.Alias),
                _ => throw new ArgumentException($"Item de lote desconhecido: {item.GetType().Name}.", nameof(itens)),
            };

            if (atribuicao is AtribuicaoAlias atribuida)
                atribuicoes.Add(atribuida);
        }

        if (OutroDono(beneficiariosDoUsuario, atribuicoes) is Beneficiario dono)
            return new ErroAliasBeneficiarioJaAtribuido(dono.Id);

        foreach ((Beneficiario destino, Alias alias) in atribuicoes)
            destino.AdicionarAlias(alias);

        return novos;
    }

    /// <summary>Cria o beneficiário do item e o guarda em <paramref name="novos"/>.</summary>
    /// <returns>A atribuição do alias do item, para a conferência dos aliases a enxergar; nula quando ele não tem alias.</returns>
    private static AtribuicaoAlias? CriarNovo(IdUsuario idUsuario, BeneficiarioNovo novo, List<Beneficiario> novos)
    {
        Beneficiario criado = Beneficiario.Criar(idUsuario, novo.Nome);
        novos.Add(criado);

        return novo.Alias is null ? null : (criado, novo.Alias);
    }

    /// <summary>
    /// O beneficiário que já tem o alias de uma atribuição, ou que o recebe numa atribuição anterior do lote, quando não
    /// é o destino dela.
    /// </summary>
    private static Beneficiario? OutroDono(
        IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario,
        List<AtribuicaoAlias> atribuicoes)
    {
        Dictionary<Alias, Beneficiario> donos = [];

        foreach ((Beneficiario destino, Alias alias) in atribuicoes)
        {
            Beneficiario? dono = donos.GetValueOrDefault(alias)
                ?? beneficiariosDoUsuario.FirstOrDefault(b => b.PossuiAlias(alias));

            if (dono is not null && dono != destino)
                return dono;

            donos[alias] = destino;
        }

        return null;
    }

    private static void GarantirDoUsuario(IdUsuario idUsuario, IReadOnlyCollection<Beneficiario> beneficiariosDoUsuario)
    {
        if (beneficiariosDoUsuario.Any(b => b.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um beneficiário de outro usuário.", nameof(beneficiariosDoUsuario));
    }
}
