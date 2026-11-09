using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.WebApi.Endpoints.Beneficiarios.CadastrarLote;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;
using ControleFinanceiro.WebApi.Endpoints.Despesas.GerarDoMes;
using ControleFinanceiro.WebApi.Endpoints.Despesas.VincularTransacao;
using ControleFinanceiro.WebApi.Tests.Importacoes;
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

    /// <summary>A descrição de uma compra à vista da fatura fictícia do C6.</summary>
    public const string CompraAVista = "MERCADO FICTICIO";

    /// <summary>A descrição da compra parcelada da fatura fictícia do C6.</summary>
    public const string CompraParcelada = "LOJA DE EXEMPLO";

    /// <summary>A descrição de outra compra à vista do final físico da fatura fictícia do C6, no mesmo mês de vencimento.</summary>
    public const string CompraInternacional = "SERVICO INTERNACIONAL FICTICIO";

    /// <summary>A descrição da compra à vista do final virtual que aparece duas vezes na fatura fictícia do C6.</summary>
    public const string CompraRepetidaDoVirtual = "APLICATIVO DE TRANSPORTE FICTICIO";

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

        /// <summary>
        /// Uma despesa do mês em cada final da fatura fictícia, gerada de uma recorrente no crédito, pendente: a do
        /// físico e a do virtual, que saem na mesma fatura.
        /// </summary>
        public async Task<(DespesaAdiada Fisico, DespesaAdiada Virtual)> GerarAdiadaEmCadaFinalAsync(
            Guid idUsuario,
            CadastrarLoteBeneficiariosRequest beneficiarios,
            CadastrarDespesaRecorrenteRequest recorrente,
            GerarDespesasDoMesRequest geracao)
        {
            IReadOnlyList<Cartao> cartoes = (await api.ObterTodosAsync(db => db.Cartoes))
                .Where(c => c.IdUsuario == IdUsuario.From(idUsuario))
                .ToList();
            Cartao fisico = cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoFisico);
            Cartao @virtual = cartoes.Single(c => c.Sufixo == FaturaCartaoC6.SufixoVirtual);
            IReadOnlyList<Guid> idsBeneficiarios = await api.CadastrarBeneficiariosAsync(idUsuario, beneficiarios);
            Guid idCategoria = (await api.ObterTodosAsync(db => db.Categorias))
                .First(c => c.IdUsuario == IdUsuario.From(idUsuario))
                .Id.Value;
            CadastrarDespesaRecorrenteRequest noCredito = recorrente with { FormaPagamento = FormaPagamento.Credito.Value, IdCategoria = idCategoria };
            await api.CadastrarDespesaRecorrenteAsync(idUsuario, noCredito with { IdBeneficiario = idsBeneficiarios[0], IdCartao = fisico.Id.Value });
            await api.CadastrarDespesaRecorrenteAsync(idUsuario, noCredito with { IdBeneficiario = idsBeneficiarios[1], IdCartao = @virtual.Id.Value });
            await api.GerarDoMesAsync(idUsuario, geracao);

            IReadOnlyList<DespesaAdiada> geradas = (await api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
                .Where(d => d.IdDespesaRecorrente is not null)
                .ToList();
            return (geradas.Single(d => d.IdCartao == fisico.Id), geradas.Single(d => d.IdCartao == @virtual.Id));
        }

        /// <summary>
        /// A fatura fictícia do C6 importada depois de gerada, no final físico, a despesa adiada que prevê a compra: a
        /// importação não cria despesa para ela, e a compra fica livre para o vínculo. As demais compras à vista ganham
        /// uma adiada já cobrada.
        /// </summary>
        /// <returns>A adiada prevista, pendente e sem cobrança.</returns>
        public async Task<DespesaAdiada> ImportarFaturaComPrevisaoAsync(
            Guid idUsuario,
            string descricaoCompra,
            CadastrarDespesaRecorrenteRequest recorrente,
            GerarDespesasDoMesRequest geracao)
        {
            LayoutArquivoImportacao layout = LayoutArquivoImportacao.C6CartaoCsv;
            byte[] arquivo = FaturaCartaoC6.Completo();
            await api.ResolverCartoesDaFaturaAsync(idUsuario, layout, arquivo);
            await api.ResolverBeneficiariosDaFaturaAsync(idUsuario, layout, arquivo);
            await api.ResolverCategoriasDaFaturaAsync(idUsuario, layout, arquivo);

            Cartao fisico = (await api.ObterTodosAsync(db => db.Cartoes))
                .Single(c => c.IdUsuario == IdUsuario.From(idUsuario) && c.Sufixo == FaturaCartaoC6.SufixoFisico);
            Beneficiario beneficiario = (await api.ObterTodosAsync(db => db.Beneficiarios))
                .Single(b => b.IdUsuario == IdUsuario.From(idUsuario) && b.PossuiAlias((Alias)descricaoCompra));
            Guid idCategoria = (await api.ObterTodosAsync(db => db.Categorias))
                .First(c => c.IdUsuario == IdUsuario.From(idUsuario))
                .Id.Value;
            await api.CadastrarDespesaRecorrenteAsync(idUsuario, recorrente with
            {
                FormaPagamento = FormaPagamento.Credito.Value,
                IdCartao = fisico.Id.Value,
                IdBeneficiario = beneficiario.Id.Value,
                IdCategoria = idCategoria,
            });
            MesReferencia vencimento = FaturaCartaoC6.VencimentoComprasDeMarco;
            await api.GerarDoMesAsync(idUsuario, geracao with { Ano = vencimento.Ano, Mes = vencimento.Mes });
            await api.ImportarFaturaComSucessoAsync(idUsuario, layout, arquivo);

            return (await api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
                .Single(d => d.IdUsuario == IdUsuario.From(idUsuario) && d.IdDespesaRecorrente is not null);
        }

        /// <summary>A despesa adiada que a importação da fatura criou para a compra à vista, já cobrada por ela.</summary>
        public async Task<DespesaAdiada> ObterAdiadaDaCompraAsync(Guid idUsuario, string descricaoCompra)
        {
            Transacao compra = await api.ObterTransacaoAsync(idUsuario, descricaoCompra);

            return (await api.ObterTodosAsync(db => db.Despesas.OfType<DespesaAdiada>()))
                .Single(d => d.Cobranca != null && d.Cobranca.IdTransacao == compra.Id);
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
            await api.GerarDoMesAsync(idUsuario, geracao);
        }

        private async Task GerarDoMesAsync(Guid idUsuario, GerarDespesasDoMesRequest geracao)
        {
            using HttpResponseMessage gerada = await api.Client.PostAsJsonAsync(
                new Uri($"/api/usuarios/{idUsuario}/despesas/geracoes", UriKind.Relative),
                geracao);
            gerada.StatusCode.Should().Be(HttpStatusCode.OK, await gerada.Content.ReadAsStringAsync());
        }
    }
}
