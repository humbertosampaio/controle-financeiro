using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Cria a despesa recorrente, com uma entrada por forma de pagamento: o crédito exige o cartão, e as outras formas não
/// o aceitam. Recusa a despesa do mesmo beneficiário e com a mesma descrição de uma recorrente do usuário, sem contar
/// caixa, acentos e espaços repetidos. A despesa guarda só os ids do cartão, do beneficiário e da categoria.
/// </summary>
/// <remarks>
/// O cartão, o beneficiário e a categoria chegam carregados por uma consulta restrita ao usuário: um deles de outro
/// usuário é erro de quem chama, e não uma falha prevista.
/// </remarks>
public static class DespesaRecorrenteFactory
{
    /// <param name="recorrentesDoUsuario">Todas as despesas recorrentes do usuário, e só as dele.</param>
    /// <exception cref="ArgumentException">
    /// Quando a lista traz uma recorrente de outro usuário, ou o beneficiário, a categoria ou o cartão é de outro
    /// usuário.
    /// </exception>
    public static Result<DespesaRecorrente, Erro> CriarNoCredito(
        IdUsuario idUsuario,
        IReadOnlyCollection<DespesaRecorrente> recorrentesDoUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        Cartao cartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        GarantirDoUsuario(idUsuario, cartao, nameof(cartao));

        return Criar(
            idUsuario,
            recorrentesDoUsuario,
            beneficiario,
            descricao,
            categoria,
            FormaPagamento.Credito,
            cartao.Id,
            valor,
            diaOcorrencia);
    }

    /// <param name="recorrentesDoUsuario">Todas as despesas recorrentes do usuário, e só as dele.</param>
    /// <exception cref="ArgumentException">
    /// Quando a forma de pagamento é o crédito, a lista traz uma recorrente de outro usuário, ou o beneficiário ou a
    /// categoria é de outro usuário.
    /// </exception>
    public static Result<DespesaRecorrente, Erro> CriarSemCartao(
        IdUsuario idUsuario,
        IReadOnlyCollection<DespesaRecorrente> recorrentesDoUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        FormaPagamento formaPagamento,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        if (formaPagamento == FormaPagamento.Credito)
            throw new ArgumentException("A despesa recorrente no crédito precisa do cartão.", nameof(formaPagamento));

        return Criar(
            idUsuario,
            recorrentesDoUsuario,
            beneficiario,
            descricao,
            categoria,
            formaPagamento,
            idCartao: null,
            valor,
            diaOcorrencia);
    }

    private static Result<DespesaRecorrente, Erro> Criar(
        IdUsuario idUsuario,
        IReadOnlyCollection<DespesaRecorrente> recorrentesDoUsuario,
        Beneficiario beneficiario,
        TextoObrigatorio descricao,
        CategoriaDespesa categoria,
        FormaPagamento formaPagamento,
        IdCartao? idCartao,
        ReaisPositivo valor,
        DiaOcorrenciaRecorrente diaOcorrencia)
    {
        GarantirDoUsuario(idUsuario, beneficiario, nameof(beneficiario));
        GarantirDoUsuario(idUsuario, categoria, nameof(categoria));

        if (recorrentesDoUsuario.Any(r => r.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma despesa recorrente de outro usuário.", nameof(recorrentesDoUsuario));

        if (recorrentesDoUsuario.FirstOrDefault(r => r.PossuiBeneficiarioEDescricao(beneficiario.Id, descricao)) is DespesaRecorrente repetida)
            return new ErroDespesaRecorrenteJaCadastrada(repetida.Id);

        return DespesaRecorrente.Criar(
            idUsuario,
            beneficiario.Id,
            descricao,
            categoria.Id,
            formaPagamento,
            idCartao,
            valor,
            diaOcorrencia);
    }

    private static void GarantirDoUsuario(IdUsuario idUsuario, IPertenceAoUsuario agregado, string parametro)
    {
        if (agregado.IdUsuario != idUsuario)
            throw new ArgumentException("O agregado é de outro usuário.", parametro);
    }
}
