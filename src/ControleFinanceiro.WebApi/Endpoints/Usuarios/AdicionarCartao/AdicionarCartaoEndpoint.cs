using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

public sealed class AdicionarCartaoEndpoint : Endpoint<AdicionarCartaoRequest, AdicionarCartaoResponse, AdicionarCartaoMapper>
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdicionarCartaoEndpoint(
        IUsuarioRepository usuarioRepository,
        ICartaoRepository cartaoRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioRepository = usuarioRepository;
        _cartaoRepository = cartaoRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/cartoes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AdicionarCartaoRequest req, CancellationToken ct)
    {
        IdUsuario idUsuario = IdUsuario.From(req.IdUsuario);

        Maybe<Usuario> usuario = await _usuarioRepository.ObterPorIdAsync(idUsuario, ct);
        if (usuario.HasNoValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        IReadOnlyList<Cartao> cartoesDoUsuario = await _cartaoRepository.ListarDoUsuarioAsync(idUsuario, ct);

        Result<Cartao, Erro> criado = CartaoFactory.Criar(
            idUsuario,
            cartoesDoUsuario,
            Banco.FromValue(req.Banco),
            (SufixoCartao)req.Sufixo,
            (DiaVencimentoCartao)req.DiaVencimento);

        if (!criado.TryGetValue(out Cartao? cartao, out Erro? erro))
        {
            await EnviarErroAsync(erro, ct);
            return;
        }

        await _cartaoRepository.AddAsync(cartao, ct);
        await _unitOfWork.CommitAsync(ct);

        await SendMapped(cartao, StatusCodes.Status201Created, ct);
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroCartaoJaCadastrado:
                AddError(r => r.Sufixo, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
