using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O que o usuário precisa resolver antes de importar um arquivo já lido, em qualquer rota: os beneficiários e as
/// categorias do banco sem alias e, na fatura, os finais sem cartão. Nada é gravado.
/// </summary>
public sealed class PendenciasImportacaoService
{
    private readonly ICartaoRepository _cartaoRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly ICategoriaDespesaRepository _categoriaDespesaRepository;

    public PendenciasImportacaoService(
        ICartaoRepository cartaoRepository,
        IBeneficiarioRepository beneficiarioRepository,
        ICategoriaDespesaRepository categoriaDespesaRepository)
    {
        _cartaoRepository = cartaoRepository;
        _beneficiarioRepository = beneficiarioRepository;
        _categoriaDespesaRepository = categoriaDespesaRepository;
    }

    /// <returns>
    /// Os textos de beneficiário das saídas que ainda não são alias de nenhum beneficiário do usuário, um por forma
    /// normalizada, na ordem do arquivo.
    /// </returns>
    public async Task<IReadOnlyList<Alias>> ListarDoExtratoAsync(IdUsuario idUsuario, Extrato extrato, CancellationToken ct)
    {
        IReadOnlyList<Beneficiario> beneficiarios = await _beneficiarioRepository.ListarAsync(ct);

        return AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiarios, extrato.Lancamentos);
    }

    public async Task<PendenciasFatura> ListarDaFaturaAsync(IdUsuario idUsuario, Fatura fatura, CancellationToken ct)
    {
        IReadOnlyList<Cartao> cartoes = await _cartaoRepository.ListarAsync(ct);
        IReadOnlyList<Beneficiario> beneficiarios = await _beneficiarioRepository.ListarAsync(ct);
        IReadOnlyList<CategoriaDespesa> categorias = await _categoriaDespesaRepository.ListarAsync(ct);
        IReadOnlyList<Lancamento> lancamentos = fatura.Lancamentos.Select(l => l.Lancamento).ToArray();

        return new PendenciasFatura(
            fatura.Sufixos
                .Select(sufixo => new PendenciasFatura.FinalCartao(sufixo, CartaoFactory.ObterDoFinal(idUsuario, cartoes, fatura.Banco, sufixo)))
                .ToArray(),
            AliasesBeneficiarioPendentes.Listar(idUsuario, beneficiarios, lancamentos),
            AliasesCategoriaPendentes.Listar(idUsuario, categorias, lancamentos));
    }
}
