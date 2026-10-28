using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;

namespace ControleFinanceiro.WebApi.Tests.Despesas.CadastrarRecorrente;

/// <summary>
/// Monta o <see cref="CadastrarDespesaRecorrenteRequest"/> a partir de uma <see cref="DespesaRecorrente"/> do fixture.
/// Os ids do beneficiário, da categoria e do cartão não existem no banco: o teste troca pelos do usuário cadastrado. O
/// <see cref="CadastrarDespesaRecorrenteRequest.IdUsuario"/> fica de fora: vem da rota, que vence o corpo.
/// </summary>
internal sealed class CadastrarDespesaRecorrenteRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DespesaRecorrente despesaRecorrente) => new CadastrarDespesaRecorrenteRequest
        {
            IdBeneficiario = despesaRecorrente.IdBeneficiario,
            Descricao = despesaRecorrente.Descricao,
            IdCategoria = despesaRecorrente.IdCategoria,
            FormaPagamento = despesaRecorrente.FormaPagamento.Value,
            IdCartao = despesaRecorrente.IdCartao,
            Valor = despesaRecorrente.Valor,
            DiaOcorrencia = despesaRecorrente.DiaOcorrencia,
        });
    }
}
