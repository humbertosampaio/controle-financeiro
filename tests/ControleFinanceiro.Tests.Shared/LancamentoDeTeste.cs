using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>Deriva de um lançamento gerado o que o caso precisa, sem o teste repetir a lista de campos.</summary>
public static class LancamentoDeTeste
{
    extension(Lancamento lancamento)
    {
        /// <summary>
        /// A mesma linha no extrato de uma conta, sem a parcela e a marca de pagamento, que ele nunca traz. Já da conta, a
        /// própria linha.
        /// </summary>
        public LancamentoConta NaConta()
        {
            return lancamento as LancamentoConta
                ?? LancamentoConta.Criar(
                    lancamento.Data,
                    lancamento.Hora,
                    lancamento.Valor,
                    lancamento.Descricao,
                    lancamento.Contraparte,
                    lancamento.CategoriaBanco,
                    lancamento.Identidade);
        }

        /// <summary>
        /// A mesma linha na fatura de um cartão; a da conta vira uma linha sem parcela. Já do cartão, a própria linha, com a
        /// marca do <see cref="ComoPagamentoFaturaAnterior"/>.
        /// </summary>
        public LancamentoCartao NoCartao()
        {
            return lancamento as LancamentoCartao
                ?? LancamentoCartao.Criar(
                    lancamento.Data,
                    lancamento.Hora,
                    lancamento.Valor,
                    lancamento.Descricao,
                    lancamento.Contraparte,
                    parcela: null,
                    lancamento.CategoriaBanco,
                    lancamento.Identidade);
        }

        /// <summary>
        /// A mesma linha na fatura de um cartão, como compra ou estorno, com o valor do caso e sem categoria, para o caso cujo
        /// assunto é o total da fatura.
        /// </summary>
        public LancamentoCartao NoCartaoComValor(decimal valor)
        {
            return LancamentoCartao.Criar(
                lancamento.Data,
                lancamento.Hora,
                valor,
                lancamento.Descricao,
                lancamento.Contraparte,
                (lancamento as LancamentoCartao)?.Parcela,
                categoriaBanco: null,
                lancamento.Identidade);
        }

        /// <summary>
        /// A mesma linha no extrato de uma conta, na data e com o valor do caso e sem categoria, para o caso cujo assunto é a
        /// saída que paga a fatura.
        /// </summary>
        public LancamentoConta NaContaComValor(DateOnly data, decimal valor)
        {
            return LancamentoConta.Criar(data, lancamento.Hora, valor, lancamento.Descricao, lancamento.Contraparte, categoriaBanco: null, lancamento.Identidade);
        }
    }

    /// <remarks>
    /// Remontam a linha pelo tipo dela: a do cartão mantém a parcela e descarta a marca do <see cref="ComoPagamentoFaturaAnterior"/>,
    /// que é a última a aplicar.
    /// </remarks>
    extension<T>(T lancamento)
        where T : Lancamento
    {
        /// <summary>A mesma linha sem categoria do banco: o lote não precisa de categorias do usuário.</summary>
        public T SemCategoria() => lancamento.Montar(lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, categoriaBanco: null, lancamento.Identidade);

        /// <summary>A mesma linha com outra descrição e outra contraparte, para o caso cujo assunto são esses textos.</summary>
        public T ComTextos(TextoObrigatorio descricao, string? contraparte) =>
            lancamento.Montar(lancamento.Valor, descricao, contraparte, lancamento.CategoriaBanco, lancamento.Identidade);

        /// <summary>A mesma linha com outra identidade, para o caso cujo assunto é o identificador que o banco atribui.</summary>
        public T ComIdentidade(IdentidadeLancamento identidade) =>
            lancamento.Montar(lancamento.Valor, lancamento.Descricao, lancamento.Contraparte, lancamento.CategoriaBanco, identidade);

        /// <summary>A mesma linha como saída de dinheiro, com o valor negativo e nunca zero: só uma saída paga despesa.</summary>
        public T ComoSaida()
        {
            return lancamento.Montar(
                -(decimal.Abs(lancamento.Valor) + 1m),
                lancamento.Descricao,
                lancamento.Contraparte,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }

        /// <summary>A mesma linha como entrada de dinheiro, com o valor positivo e nunca zero, como o estorno.</summary>
        public T ComoEntrada()
        {
            return lancamento.Montar(
                decimal.Abs(lancamento.Valor) + 1m,
                lancamento.Descricao,
                lancamento.Contraparte,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }

        private T Montar(Reais valor, TextoObrigatorio descricao, string? contraparte, string? categoriaBanco, IdentidadeLancamento identidade)
        {
            Lancamento montado = lancamento switch
            {
                LancamentoCartao cartao => LancamentoCartao.Criar(cartao.Data, cartao.Hora, valor, descricao, contraparte, cartao.Parcela, categoriaBanco, identidade),
                LancamentoConta conta => LancamentoConta.Criar(conta.Data, conta.Hora, valor, descricao, contraparte, categoriaBanco, identidade),
                _ => throw new ArgumentOutOfRangeException(nameof(lancamento), lancamento, message: null),
            };

            return (T)montado;
        }
    }

    extension(LancamentoCartao lancamento)
    {
        /// <summary>
        /// A mesma linha na fatura, na data e com a parcela do caso, para o caso cujo assunto é a data da compra diante do
        /// vencimento.
        /// </summary>
        public LancamentoCartao NaFatura(DateOnly data, Parcela? parcela)
        {
            return LancamentoCartao.Criar(
                data,
                lancamento.Hora,
                lancamento.Valor,
                lancamento.Descricao,
                lancamento.Contraparte,
                parcela,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }

        /// <summary>
        /// A mesma linha como o pagamento da fatura anterior: com o <see cref="Lancamento.ValorAbsoluto"/> e sem parcela. É o
        /// último a aplicar: o <c>SemCategoria</c>, o <c>ComTextos</c>, o <c>ComIdentidade</c> e o <c>ComoSaida</c> remontam a
        /// linha pelo <see cref="LancamentoCartao.Criar"/> e descartam a marca.
        /// </summary>
        public LancamentoCartao ComoPagamentoFaturaAnterior()
        {
            return LancamentoCartao.PagamentoFaturaAnterior(
                lancamento.Data,
                lancamento.Hora,
                lancamento.ValorAbsoluto,
                lancamento.Descricao,
                lancamento.Contraparte,
                lancamento.CategoriaBanco,
                lancamento.Identidade);
        }
    }
}
