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
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdicionarCartaoEndpoint(
        IUsuarioAtual usuarioAtual,
        ICartaoRepository cartaoRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
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
        IdUsuario idUsuario = _usuarioAtual.Id;

        IReadOnlyList<Cartao> cartoesDoUsuario = await _cartaoRepository.ListarAsync(ct);

        Result<Cartao, Erro> criado = CartaoFactory.Criar(
            idUsuario,
            cartoesDoUsuario,
            Banco.FromValue(req.Banco),
            (SufixoCartao)req.Sufixo,
            (DiaVencimentoCartao)req.DiaVencimento,
            (DiaFechamentoCartao)req.DiaFechamento);

        if (!criado.TryGetValue(out Cartao? cartao, out Erro? erro))
        {
            await EnviarErroAsync(erro, ct);
            return;
        }

        await _cartaoRepository.AdicionarAsync(cartao, ct);
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
