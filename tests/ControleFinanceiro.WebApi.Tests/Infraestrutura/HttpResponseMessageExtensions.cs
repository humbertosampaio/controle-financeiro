using FastEndpoints;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Infraestrutura;

internal static class HttpResponseMessageExtensions
{
    extension(HttpResponseMessage resposta)
    {
        /// <summary>
        /// Os campos com erro no <see cref="ProblemDetails"/> da resposta, sem repetir, na ordem em que aparecem. É o
        /// formato que a API usa (<c>UseProblemDetails</c>); um erro que não é de campo vem como <c>generalErrors</c>.
        /// </summary>
        public async Task<IReadOnlyList<string>> CamposComErroAsync()
        {
            ProblemDetails? problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();

            return problema!.Errors
                .Select(erro => erro.Name)
                .Distinct()
                .ToList();
        }
    }
}
