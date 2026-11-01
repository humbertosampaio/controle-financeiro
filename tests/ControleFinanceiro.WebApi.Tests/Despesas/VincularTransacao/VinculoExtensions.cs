using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes.ImportarFatura;
using ControleFinanceiro.WebApi.Tests.Infraestrutura;
using System.Net;
using System.Net.Http.Json;

namespace ControleFinanceiro.WebApi.Tests.Despesas.VincularTransacao;

/// <summary>Os cenários de vínculo pelas rotas, compartilhados pelos testes de vincular, desvincular e listar.</summary>
internal static class VinculoExtensions
{
    /// <summary>O início da descrição de uma saída do extrato fictício do C6.</summary>
    public const string PixEnviado = "Pix enviado";

    /// <summary>A descrição da compra parcelada da fatura fictícia do C6.</summary>
    public const string CompraParcelada = "LOJA DE EXEMPLO";

    extension(ApiComPostgresFixture api)
    {
        public Task<HttpResponseMessage> VincularAsync(Guid idUsuario, Guid idDespesa, VincularTransacaoRequest requisicao)
        {
            return api.Client.PostAsJsonAsync(
                new Uri($"/api/usuarios/{idUsuario}/despesas/{idDespesa}/vinculos", UriKind.Relative),
                requisicao);
        }

        public async Task VincularComSucessoAsync(Guid idUsuario, Guid idDespesa, Guid idTransacao)
        {
            using HttpResponseMessage resposta = await api.VincularAsync(idUsuario, idDespesa, new VincularTransacaoRequest { IdTransacao = idTransacao });
            resposta.StatusCode.Should().Be(HttpStatusCode.OK, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>A despesa do mês gerada de uma recorrente no Pix, pendente.</summary>
        public async Task<DespesaSimples> GerarSimplesAsync(
            Guid idUsuario,
            CadastrarLoteBeneficiariosRequest beneficiarios,
            CadastrarDespesaRecorrenteRequest recorrente,
            GerarDespesasDoMesRequest geracao)
        {
            await api.CadastrarRecorrenteEGerarAsync(idUsuario, beneficiarios, recorrente with { FormaPagamento = FormaPagamento.Pix.Value, IdCartao = null }, geracao);

            return (await api.ObterTodosAsync(db => db.Despesas.OfType<DespesaSimples>()))
                .Single(d => d.IdUsuario == IdUsuario.From(idUsuario));
        }

        /// <summary>A despesa do mês gerada de uma recorrente no crédito do cartão físico da fatura fictícia, pendente.</summary>
        public async Task<DespesaAdiada> GerarAdiadaAsync(
            Guid idUsuario,
            CadastrarLoteBeneficiariosRequest beneficiarios,
            CadastrarDespesaRecorrenteRequest recorrente,
            GerarDespesasDoMesRequest geracao)
        {
            Cartao fisico = (await api.ObterTodosAsync(db => db.Cartoes))
                .Single(c => c.IdUsuario == IdUsuario.From(idUsuario) && c.Sufixo == FaturaCartaoC6.SufixoFisico);
            await api.CadastrarRecorrenteEGerarAsync(idUsuario, beneficiarios, recorrente with { FormaPagamento = FormaPagamento.Credito.Value, IdCartao = fisico.Id.Value }, geracao);

            return (await api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
                .Single(d => d.IdUsuario == IdUsuario.From(idUsuario));
        }

        public async Task<Transacao> ObterTransacaoAsync(Guid idUsuario, string descricao)
        {
            return (await api.ObterTodosAsync(db => db.Transacoes))
                .First(t => t.IdUsuario == IdUsuario.From(idUsuario) && t.Descricao.Value.StartsWith(descricao, StringComparison.Ordinal));
        }

        private async Task CadastrarRecorrenteEGerarAsync(
            Guid idUsuario,
            CadastrarLoteBeneficiariosRequest beneficiarios,
            CadastrarDespesaRecorrenteRequest recorrente,
            GerarDespesasDoMesRequest geracao)
        {
            IReadOnlyList<Guid> idsBeneficiarios = await api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
            Guid idCategoria = (await api.ObterTodosAsync(db => db.Categorias))
                .First(c => c.IdUsuario == IdUsuario.From(idUsuario))
                .Id.Value;
            await api.CadastrarDespesaRecorrenteAsync(idUsuario, recorrente with { IdBeneficiario = idsBeneficiarios[0], IdCategoria = idCategoria });

            using HttpResponseMessage gerada = await api.Client.PostAsJsonAsync(
                new Uri($"/api/usuarios/{idUsuario}/despesas/geracoes", UriKind.Relative),
                geracao);
            gerada.StatusCode.Should().Be(HttpStatusCode.OK, await gerada.Content.ReadAsStringAsync());
        }
    }
}
