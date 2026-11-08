using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.PagarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.PagarFatura;

/// <summary>O pagamento da fatura pela rota, compartilhado pelos testes de pagar a fatura e de desvincular.</summary>
internal static class PagamentoFaturaExtensions
{
    extension(PagarFaturaRequest requisicao)
    {
        /// <summary>A requisição do fixture apontada para a saída, um cartão da fatura e o mês de vencimento do teste.</summary>
        public PagarFaturaRequest Pagando(Transacao saida, IdCartao idCartao, MesReferencia mesVencimento)
        {
            return requisicao with
            {
                IdTransacao = saida.Id.Value,
                IdCartao = idCartao.Value,
                Ano = mesVencimento.Ano,
                Mes = mesVencimento.Mes,
            };
        }
    }

    extension(ApiComPostgresFixture api)
    {
        public Task<HttpResponseMessage> PagarFaturaAsync(Guid idUsuario, PagarFaturaRequest requisicao)
        {
            return api.Client.PostAsJsonAsync(new Uri($"/api/usuarios/{idUsuario}/despesas/pagamentos-fatura", UriKind.Relative), requisicao);
        }

        public async Task PagarFaturaComSucessoAsync(Guid idUsuario, PagarFaturaRequest requisicao)
        {
            using HttpResponseMessage resposta = await api.PagarFaturaAsync(idUsuario, requisicao);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        }
    }
}
