using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Domain.Shared.Extensions;

public static class ReaisExtensions
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        /// <summary>
        /// Soma os valores positivos da sequência, sem sair do <see cref="ReaisPositivo"/>: a soma de positivos é
        /// sempre positiva, e a sequência vazia soma zero.
        /// </summary>
        public ReaisPositivo Sum(Func<TSource, ReaisPositivo> selector) => (ReaisPositivo)Enumerable.Sum(source, item => selector(item));
    }
}
