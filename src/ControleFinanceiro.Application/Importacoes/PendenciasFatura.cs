using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Application.Importacoes;

/// <summary>
/// O que o usuário precisa preencher ou resolver antes de importar a fatura, para quem pediu a prévia montar a própria
/// resposta.
/// </summary>
/// <param name="Finais">Os finais de cartão do arquivo, na ordem em que aparecem pela primeira vez.</param>
/// <param name="Beneficiarios">
/// Os textos de beneficiário das compras que ainda não são alias de nenhum beneficiário do usuário, um por forma
/// normalizada, na ordem do arquivo.
/// </param>
/// <param name="CategoriasBanco">
/// As categorias que o banco deu aos lançamentos que ainda não são alias de nenhuma categoria do usuário, uma por forma
/// normalizada, na ordem do arquivo. Vazia no layout que não traz categoria.
/// </param>
public sealed record PendenciasFatura(
    IReadOnlyList<PendenciasFatura.FinalCartao> Finais,
    IReadOnlyList<Alias> Beneficiarios,
    IReadOnlyList<Alias> CategoriasBanco)
{
    /// <param name="Cartao">O cartão do usuário com esse final no banco da fatura; vazio quando ele precisa cadastrá-lo antes de importar.</param>
    public sealed record FinalCartao(SufixoCartao Sufixo, Maybe<Cartao> Cartao);
}
