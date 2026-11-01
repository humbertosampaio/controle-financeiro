using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Tests.Services;

/// <summary>
/// Os beneficiários que deixam resolvidos os textos de beneficiário de um lote, para o teste que não trata deles criar
/// as transações sem falhar com <see cref="ErroBeneficiariosNaoResolvidos"/>.
/// </summary>
internal static class BeneficiariosResolvidos
{
    /// <returns>Um beneficiário do usuário por texto pendente, com o próprio texto como nome e alias.</returns>
    public static IReadOnlyList<Beneficiario> Dos(IdUsuario idUsuario, IEnumerable<Lancamento> lancamentos)
    {
        IEnumerable<BeneficiarioNovo> novos = AliasesBeneficiarioPendentes
            .Listar(idUsuario, beneficiariosDoUsuario: [], lancamentos)
            .Select(alias => new BeneficiarioNovo((TextoObrigatorio)alias.TextoOriginal, alias));

        return BeneficiarioFactory.Resolver(idUsuario, beneficiariosDoUsuario: [], novos).Value;
    }
}
