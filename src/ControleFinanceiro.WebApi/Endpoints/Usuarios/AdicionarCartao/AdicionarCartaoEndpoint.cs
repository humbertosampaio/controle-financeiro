using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

public sealed class AdicionarCartaoEndpoint : Endpoint<AdicionarCartaoRequest, AdicionarCartaoResponse, AdicionarCartaoMapper>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdicionarCartaoEndpoint(IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/cartoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AdicionarCartaoRequest req, CancellationToken ct)
    {
        Maybe<Usuario> encontrado = await _usuarioRepository.ObterPorIdAsync(IdUsuario.From(req.IdUsuario), ct);
        if (!encontrado.TryGetValue(out Usuario? usuario))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        Cartao cartao = Map.ToEntity(req);

        // Falha única: Usuario.AdicionarCartao. Toda falha vira 409 no campo sufixo. Se o método ganhar outra falha, troque por erro tipado.
        Result adicionado = usuario.AdicionarCartao(cartao);
        if (adicionado.IsFailure)
        {
            AddError(r => r.Sufixo, adicionado.Error);
            await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
            return;
        }

        await _unitOfWork.CommitAsync(ct);

        await SendMapped(cartao, StatusCodes.Status201Created, ct);
    }
}
