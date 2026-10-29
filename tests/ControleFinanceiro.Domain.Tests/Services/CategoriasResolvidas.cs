using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Tests.Services;

/// <summary>
/// As categorias que deixam resolvidas as categorias do banco de um lote, para o teste que não trata delas criar as
/// transações sem falhar com <see cref="ErroCategoriasBancoNaoResolvidas"/>.
/// </summary>
internal static class CategoriasResolvidas
{
    /// <returns>Uma categoria do usuário por texto pendente, com o próprio texto como nome e alias.</returns>
    public static IReadOnlyList<CategoriaDespesa> Dos(IdUsuario idUsuario, IEnumerable<Lancamento> lancamentos)
    {
        IEnumerable<CategoriaNova> novas = AliasesCategoriaPendentes
            .Listar(idUsuario, categoriasDoUsuario: [], lancamentos)
            .Select(alias => new CategoriaNova((TextoObrigatorio)alias.TextoOriginal, alias));

        return CategoriaDespesaFactory.Resolver(idUsuario, categoriasDoUsuario: [], novas).Value;
    }
}
