using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.Listar;

/// <summary>Os beneficiários do usuário com os aliases de cada um, para a tela da resolução e a do cadastro.</summary>
public sealed class ListarBeneficiariosEndpoint : Endpoint<ListarBeneficiariosRequest, ListarBeneficiariosResponse>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;

    public ListarBeneficiariosEndpoint(
        IUsuarioAtual usuarioAtual,
        IUsuarioRepository usuarioRepository,
        IBeneficiarioRepository beneficiarioRepository)
    {
        _usuarioAtual = usuarioAtual;
        _usuarioRepository = usuarioRepository;
        _beneficiarioRepository = beneficiarioRepository;
    }

    public override void Configure()
    {
        Get("usuarios/{idUsuario}/beneficiarios");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListarBeneficiariosRequest req, CancellationToken ct)
    {
        Maybe<Usuario> usuario = await _usuarioRepository.ObterPorIdAsync(_usuarioAtual.Id, ct);
        if (usuario.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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
