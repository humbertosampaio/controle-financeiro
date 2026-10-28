using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;

/// <summary>
/// Cadastra uma despesa recorrente do usuário. O beneficiário, a categoria e o cartão vêm dos repositórios, que só
/// enxergam os do usuário: o que não está entre eles responde 422.
/// </summary>
public sealed class CadastrarDespesaRecorrenteEndpoint
    : Endpoint<CadastrarDespesaRecorrenteRequest, CadastrarDespesaRecorrenteResponse, CadastrarDespesaRecorrenteMapper>
{
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaRepository;
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IDespesaRecorrenteRepository _despesaRecorrenteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CadastrarDespesaRecorrenteEndpoint(
        IUsuarioAtual usuarioAtual,
        IBeneficiarioRepository beneficiarioRepository,
        ICategoriaDespesaRepository categoriaRepository,
        ICartaoRepository cartaoRepository,
        IDespesaRecorrenteRepository despesaRecorrenteRepository,
        IUnitOfWork unitOfWork)
    {
        _usuarioAtual = usuarioAtual;
        _beneficiarioRepository = beneficiarioRepository;
        _categoriaRepository = categoriaRepository;
        _cartaoRepository = cartaoRepository;
        _despesaRecorrenteRepository = despesaRecorrenteRepository;
        _unitOfWork = unitOfWork;
    }

    public override void Configure()
    {
        Post("usuarios/{idUsuario}/despesas-recorrentes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CadastrarDespesaRecorrenteRequest req, CancellationToken ct)
    {
        Maybe<Beneficiario> beneficiario = await _beneficiarioRepository.ObterAsync(IdBeneficiario.From(req.IdBeneficiario), ct);
        Maybe<CategoriaDespesa> categoria = await _categoriaRepository.ObterAsync(IdCategoriaDespesa.From(req.IdCategoria), ct);
        Maybe<Cartao> cartao = req.IdCartao is Guid idCartao
            ? await _cartaoRepository.ObterAsync(IdCartao.From(idCartao), ct)
            : Maybe<Cartao>.None;

        if (beneficiario.HasNoValue)
            AddError(r => r.IdBeneficiario, "O beneficiário não está entre os do usuário");

        if (categoria.HasNoValue)
            AddError(r => r.IdCategoria, "A categoria não está entre as do usuário");

        if (req.IdCartao is not null && cartao.HasNoValue)
            AddError(r => r.IdCartao, "O cartão não está entre os do usuário");

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(StatusCodes.Status422UnprocessableEntity, ct);
            return;
        }

        IReadOnlyList<DespesaRecorrente> recorrentesDoUsuario = await _despesaRecorrenteRepository.ListarAsync(ct);
        Result<DespesaRecorrente, Erro> criada = Criar(req, recorrentesDoUsuario, beneficiario.Value, categoria.Value, cartao);

        if (!criada.TryGetValue(out DespesaRecorrente? despesaRecorrente, out Erro? erro))
        {
            await EnviarErroAsync(erro, ct);
            return;
        }

        await _despesaRecorrenteRepository.AdicionarAsync(despesaRecorrente, ct);
        await _unitOfWork.CommitAsync(ct);

        await SendMapped(despesaRecorrente, StatusCodes.Status201Created, ct);
    }

    /// <summary>O validator garante o cartão no crédito, e só nele: com o cartão, a despesa é no crédito.</summary>
    private Result<DespesaRecorrente, Erro> Criar(
        CadastrarDespesaRecorrenteRequest req,
        IReadOnlyList<DespesaRecorrente> recorrentesDoUsuario,
        Beneficiario beneficiario,
        CategoriaDespesa categoria,
        Maybe<Cartao> cartao)
    {
        IdUsuario idUsuario = _usuarioAtual.Id;
        TextoObrigatorio descricao = (TextoObrigatorio)req.Descricao;
        ReaisPositivo valor = (ReaisPositivo)req.Valor;
        DiaOcorrenciaRecorrente diaOcorrencia = (DiaOcorrenciaRecorrente)req.DiaOcorrencia;

        if (cartao.TryGetValue(out Cartao? cartaoDoUsuario))
        {
            return DespesaRecorrenteFactory.CriarNoCredito(
                idUsuario, recorrentesDoUsuario, beneficiario, descricao, categoria, cartaoDoUsuario, valor, diaOcorrencia);
        }

        return DespesaRecorrenteFactory.CriarSemCartao(
            idUsuario,
            recorrentesDoUsuario,
            beneficiario,
            descricao,
            categoria,
            FormaPagamento.FromValue(req.FormaPagamento),
            valor,
            diaOcorrencia);
    }

    private async Task EnviarErroAsync(Erro erro, CancellationToken ct)
    {
        switch (erro)
        {
            case ErroDespesaRecorrenteJaCadastrada:
                AddError(r => r.Descricao, erro.Descricao);
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, ct);
                break;

            default:
                throw new InvalidOperationException($"Erro sem status mapeado: {erro}");
        }
    }
}
