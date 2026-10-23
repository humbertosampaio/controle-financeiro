using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios;

public sealed partial class CadastrarUsuarioEndpoint : Endpoint<CadastrarUsuarioRequest, CadastrarUsuarioResponse, CadastrarUsuarioMapper>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarUsuarioEndpoint(IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CadastrarUsuarioRequest req, CancellationToken ct)
    {
        Usuario usuario = Map.ToEntity(req);

        await _usuarioRepository.AdicionarAsync(usuario, ct);
        await _unitOfWork.CommitAsync(ct);

        await SendMapped(usuario, StatusCodes.Status201Created, ct);
    }
}