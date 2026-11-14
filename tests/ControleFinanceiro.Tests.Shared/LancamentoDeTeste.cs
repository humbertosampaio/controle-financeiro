using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>Deriva de um lançamento gerado o que o caso precisa, sem o teste repetir a lista de campos.</summary>
public static class LancamentoDeTeste
{
    extension(Lancamento lancamento)
    {
        /// <summary>
        /// A mesma linha, como se viesse do tipo de origem pedido. No extrato, sem a parcela, que ele nunca traz. Já no tipo
        /// pedido, a própria linha, com a marca do <see cref="ComoPagamentoFatura"/>.
        /// </summary>
        public Lancamento NaOrigem(TipoOrigem tipoOrigem)
        {
            return lancamento.TipoOrigem == tipoOrigem
                ? lancamento
                : lancamento.Montar(tipoOrigem, lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco);
        }

        /// <summary>A mesma linha sem categoria do banco: o lote não precisa de categorias do usuário.</summary>
        public Lancamento SemCategoria() => lancamento.Montar(lancamento.TipoOrigem, lancamento.Descricao, lancamento.Contraparte, categoriaBanco: null);

        /// <summary>A mesma linha com outra descrição e outra contraparte, para o caso cujo assunto são esses textos.</summary>
        public Lancamento ComTextos(TextoObrigatorio descricao, string? contraparte) =>
            lancamento.Montar(lancamento.TipoOrigem, descricao, contraparte, lancamento.CategoriaBanco);

        /// <summary>A mesma linha com outra identidade, para o caso cujo assunto é o identificador que o banco atribui.</summary>
        public Lancamento ComIdentidade(IdentidadeLancamento identidade) =>
            lancamento.Montar(lancamento.TipoOrigem, lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco, identidade);

        /// <summary>A mesma linha como saída de dinheiro, com o valor negativo e nunca zero: só uma saída paga despesa.</summary>
        public Lancamento ComoSaida()
        {
            return lancamento.Montar(
                lancamento.TipoOrigem,
                -(decimal.Abs(lancamento.Valor) + 1m),
                lancamento.Descricao,
                lancamento.Contraparte,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }

        /// <summary>
        /// A mesma linha como o pagamento da fatura anterior: com o <see cref="Lancamento.ValorAbsoluto"/>, sem parcela e na
        /// origem cartão. É o último a aplicar: o <see cref="SemCategoria"/>, o <see cref="ComTextos"/>, o
        /// <see cref="ComIdentidade"/> e o <see cref="ComoSaida"/> remontam a linha pelo <c>DoCartao</c> ou pelo <c>DaConta</c> e
        /// descartam a marca.
        /// </summary>
        public Lancamento ComoPagamentoFatura()
        {
            return Lancamento.PagamentoFaturaCartao(
                lancamento.Data,
                lancamento.Hora,
                lancamento.ValorAbsoluto,
                lancamento.Descricao,
                lancamento.Contraparte,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }

        private Lancamento Montar(TipoOrigem tipoOrigem, TextoObrigatorio descricao, string? contraparte, string? categoriaBanco) =>
            lancamento.Montar(tipoOrigem, lancamento.Valor, descricao, contraparte, categoriaBanco, lancamento.Identidade);

        private Lancamento Montar(
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
}
