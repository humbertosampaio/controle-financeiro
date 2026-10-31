using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarCicloFaturaCartao;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AlterarCicloFaturaCartao;

/// <summary>
/// Monta o <see cref="AlterarCicloFaturaCartaoRequest"/> com os dias do fixture, que saem diferentes dos de um
/// cartão pedido no mesmo teste. Os ids ficam de fora: vêm da rota, que vence o
/// corpo.
/// </summary>
internal sealed class AlterarCicloFaturaCartaoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DiaVencimentoCartao diaVencimento, DiaFechamentoCartao diaFechamento) => new AlterarCicloFaturaCartaoRequest
        {
            DiaVencimento = diaVencimento,
            DiaFechamento = diaFechamento,
        });
    }
}
