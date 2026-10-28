using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Lote = (System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Beneficiarios.Beneficiario> BeneficiariosDoUsuario, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Services.IItemLoteBeneficiarios> Itens);
using Resolvido = (System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Beneficiarios.Beneficiario> Novos, System.Collections.Generic.IReadOnlyList<ControleFinanceiro.Domain.Beneficiarios.Beneficiario> Destinos);

namespace ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;

/// <summary>
/// Cadastra beneficiários e dá a eles os textos de beneficiário do arquivo como aliases, num lote aplicado inteiro ou
/// não aplicado. Serve à resolução da prévia da importação, em que o texto vira alias de um beneficiário existente ou
/// de um novo, e ao cadastro manual, em que o beneficiário novo vem sem alias.
/// </summary>
public sealed class CadastrarLoteBeneficiariosEndpoint : Endpoint<CadastrarLoteBeneficiariosRequest, CadastrarLoteBeneficiariosResponse>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarLoteBeneficiariosEndpoint(
        IUsuarioAtual usuarioAtual,
        IBeneficiarioRepository beneficiarioRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _beneficiarioRepository = beneficiarioRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/beneficiarios/lote");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CadastrarLoteBeneficiariosRequest req, CancellationToken ct)
    {
        await LerLoteAsync(req.Itens, ct)
            .Bind(Resolver)
            .Tap(resolvido => GravarAsync(resolvido.Novos, ct))
            .Match(
                resolvido => Send.OkAsync(new CadastrarLoteBeneficiariosResponse([.. resolvido.Destinos.Select(b => (Guid)b.Id)]), ct),
                falha => EnviarFalhaAsync(falha, ct));
    }

    /// <summary>O beneficiário existente que um item cita e o usuário não tem interrompe o lote.</summary>
    private async Task<Result<Lote, Falha>> LerLoteAsync(IReadOnlyList<ItemBeneficiarioDto> itens, CancellationToken ct)
    {
        IReadOnlyList<Beneficiario> beneficiariosDoUsuario = await _beneficiarioRepository.ListarAsync(ct);
        List<IItemLoteBeneficiarios> lote = [];

        foreach (ItemBeneficiarioDto item in itens)
        {
            if (item.IdBeneficiario is not Guid guid)
            {
                lote.Add(new BeneficiarioNovo((TextoObrigatorio)item.Nome, item.Alias is null ? null : (Alias)item.Alias));
                continue;
            }

            IdBeneficiario idBeneficiario = IdBeneficiario.From(guid);
            if (beneficiariosDoUsuario.FirstOrDefault(b => b.Id == idBeneficiario) is not Beneficiario beneficiario)
                return Falha.NaoEncontrado;

            lote.Add(new AliasBeneficiarioExistente(beneficiario, (Alias)item.Alias));
        }

        return (beneficiariosDoUsuario, lote);
    }

    private Result<Resolvido, Falha> Resolver(Lote lote)
    {
        return BeneficiarioFactory
            .Resolver(_usuarioAtual.Id, lote.BeneficiariosDoUsuario, lote.Itens)
            .MapError(Falha.Conflito)
            .Map(novos => (novos, Destinos(lote.Itens, novos)));
    }

    /// <summary>O beneficiário de cada item. Os novos saem da factory na ordem dos itens que os criaram.</summary>
    private static IReadOnlyList<Beneficiario> Destinos(
        IReadOnlyList<IItemLoteBeneficiarios> itens,
        IReadOnlyList<Beneficiario> novos)
    {
        Queue<Beneficiario> proximos = new(novos);
        return itens
            .Select(item => item is AliasBeneficiarioExistente existente ? existente.Beneficiario : proximos.Dequeue())
            .ToList();
    }

    private async Task GravarAsync(IReadOnlyList<Beneficiario> novos, CancellationToken ct)
    {
        await _beneficiarioRepository.AdicionarTodosAsync(novos, ct);
        await _unitOfWork.CommitAsync(ct);
    }

    private async Task EnviarFalhaAsync(Falha falha, CancellationToken ct)
    {
        switch (falha)
        {
            case Falha.Ausente:
                await Send.NotFoundAsync(ct);
                break;

            case Falha.Conflitante(ErroAliasBeneficiarioJaAtribuido erro):
                AddError(r => r.Itens, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            default:
                throw new InvalidOperationException($"Falha sem status mapeado: {falha}");
        }
    }

    /// <summary>
    /// O que interrompe o cadastro, num tipo só para a cadeia: o beneficiário de um item que não existe e o erro do
    /// domínio.
    /// </summary>
    private abstract record Falha
    {
        public static readonly Falha NaoEncontrado = new Ausente();

        public static Falha Conflito(Erro erro) => new Conflitante(erro);

        public sealed record Ausente : Falha;

        public sealed record Conflitante(Erro Erro) : Falha;
    }
}
