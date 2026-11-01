using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>Deriva de um lançamento gerado o que o caso precisa, sem o teste repetir a lista de campos.</summary>
public static class LancamentoDeTeste
{
    /// <summary>A mesma linha, como se viesse do tipo de origem pedido. No extrato, sem a parcela, que ele nunca traz.</summary>
    public static Lancamento NaOrigem(this Lancamento lancamento, TipoOrigem tipoOrigem) => Montar(lancamento, tipoOrigem, lancamento.CategoriaBanco);

    /// <summary>A mesma linha sem categoria do banco: o lote não precisa de categorias do usuário.</summary>
    public static Lancamento SemCategoria(this Lancamento lancamento) => Montar(lancamento, lancamento.TipoOrigem, categoriaBanco: null);

    private static Lancamento Montar(Lancamento lancamento, TipoOrigem tipoOrigem, string? categoriaBanco)
    {
        return tipoOrigem == TipoOrigem.Cartao
            ? Lancamento.DoCartao(lancamento.Data, lancamento.Hora, lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, lancamento.Parcela, categoriaBanco, lancamento.Identidade)
            : Lancamento.DaConta(lancamento.Data, lancamento.Hora, lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, categoriaBanco, lancamento.Identidade);
    }
}
