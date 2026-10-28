using ControleFinanceiro.Domain.Beneficiarios;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;

/// <summary>Os beneficiários do usuário com os aliases de cada um, para a tela da resolução e a do cadastro.</summary>
public sealed class ListarBeneficiariosEndpoint : Endpoint<ListarBeneficiariosRequest, ListarBeneficiariosResponse>
{
    private readonly IBeneficiarioRepository _beneficiarioRepository;

    public ListarBeneficiariosEndpoint(IBeneficiarioRepository beneficiarioRepository)
    {
        _beneficiarioRepository = beneficiarioRepository;
    }

    public override void Configure()
    {
        Get("usuarios/{idUsuario}/beneficiarios");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListarBeneficiariosRequest req, CancellationToken ct)
    {
        IReadOnlyList<Beneficiario> beneficiarios = await _beneficiarioRepository.ListarAsync(ct);

        ListarBeneficiariosResponse.Beneficiario[] beneficiariosResponse = beneficiarios
            .OrderBy(b => b.Nome.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(Ler)
            .ToArray();

        await Send.OkAsync(
            new ListarBeneficiariosResponse(beneficiariosResponse),
            ct);
    }

    private static ListarBeneficiariosResponse.Beneficiario Ler(Beneficiario beneficiario)
    {
        return new(
            beneficiario.Id,
            beneficiario.Nome,
            beneficiario.Aliases.Select(a => a.Alias.TextoOriginal).ToArray());
    }
}
