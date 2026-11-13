using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>Deriva de um lançamento gerado o que o caso precisa, sem o teste repetir a lista de campos.</summary>
public static class LancamentoDeTeste
{
    /// <summary>A mesma linha, como se viesse do tipo de origem pedido. No extrato, sem a parcela, que ele nunca traz.</summary>
    public static Lancamento NaOrigem(this Lancamento lancamento, TipoOrigem tipoOrigem) => Montar(lancamento, tipoOrigem, lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco);

    /// <summary>A mesma linha sem categoria do banco: o lote não precisa de categorias do usuário.</summary>
    public static Lancamento SemCategoria(this Lancamento lancamento) => Montar(lancamento, lancamento.TipoOrigem, lancamento.Descricao, lancamento.Contraparte, categoriaBanco: null);

    /// <summary>A mesma linha com outra descrição e outra contraparte, para o caso cujo assunto são esses textos.</summary>
    public static Lancamento ComTextos(this Lancamento lancamento, TextoObrigatorio descricao, string? contraparte) =>
        Montar(lancamento, lancamento.TipoOrigem, descricao, contraparte, lancamento.CategoriaBanco);

    /// <summary>A mesma linha com outra identidade, para o caso cujo assunto é o identificador que o banco atribui.</summary>
    public static Lancamento ComIdentidade(this Lancamento lancamento, IdentidadeLancamento identidade) =>
        Montar(lancamento, lancamento.TipoOrigem, lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco, identidade);

    /// <summary>A mesma linha como saída de dinheiro, com o valor negativo e nunca zero: só uma saída paga despesa.</summary>
    public static Lancamento ComoSaida(this Lancamento lancamento) =>
        Montar(lancamento, lancamento.TipoOrigem, -(decimal.Abs(lancamento.Valor) + 1m), lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco, lancamento.Identidade);

    private static Lancamento Montar(Lancamento lancamento, TipoOrigem tipoOrigem, TextoObrigatorio descricao, string? contraparte, string? categoriaBanco) =>
        Montar(lancamento, tipoOrigem, lancamento.Valor, descricao, contraparte, categoriaBanco, lancamento.Identidade);

    private static Lancamento Montar(
        Lancamento lancamento,
        TipoOrigem tipoOrigem,
        Reais valor,
        TextoObrigatorio descricao,
        string? contraparte,
        string? categoriaBanco,
        IdentidadeLancamento identidade)
    {
        return tipoOrigem == TipoOrigem.Cartao
            ? Lancamento.DoCartao(lancamento.Data, lancamento.Hora, valor, descricao, contraparte, lancamento.Parcela, categoriaBanco, identidade)
            : Lancamento.DaConta(lancamento.Data, lancamento.Hora, valor, descricao, contraparte, categoriaBanco, identidade);
    }
}
