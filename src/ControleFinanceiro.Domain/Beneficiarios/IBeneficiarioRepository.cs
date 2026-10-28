namespace ControleFinanceiro.Domain.Beneficiarios;

/// <summary>Enxerga só os beneficiários do usuário atual.</summary>
public interface IBeneficiarioRepository
{
    Task<IReadOnlyList<Beneficiario>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>O beneficiário de outro usuário não é encontrado, como o que não existe.</summary>
    Task<Maybe<Beneficiario>> ObterAsync(IdBeneficiario idBeneficiario, CancellationToken cancellationToken);

    Task AdicionarAsync(Beneficiario beneficiario, CancellationToken cancellationToken);

    Task AdicionarTodosAsync(IEnumerable<Beneficiario> beneficiarios, CancellationToken cancellationToken);
}
