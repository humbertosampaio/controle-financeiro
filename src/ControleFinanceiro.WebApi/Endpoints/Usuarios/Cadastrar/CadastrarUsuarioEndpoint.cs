using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.Cadastrar;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios;

public sealed partial class CadastrarUsuarioEndpoint : Endpoint<CadastrarUsuarioRequest, CadastrarUsuarioResponse, CadastrarUsuarioMapper>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarUsuarioEndpoint(
        IUsuarioRepository usuarioRepository,
        ICategoriaDespesaRepository categoriaRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _categoriaRepository = categoriaRepository;
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

        // A categoria Outros nasce do mesmo cadastro e entra no mesmo commit: o usuário nunca existe sem ela.
        await _usuarioRepository.AdicionarAsync(usuario, ct);
        await _categoriaRepository.AdicionarAsync(CategoriaDespesaFactory.CriarOutros(usuario.Id), ct);
        await _unitOfWork.CommitAsync(ct);

        await SendMapped(usuario, StatusCodes.Status201Created, ct);
    }
}