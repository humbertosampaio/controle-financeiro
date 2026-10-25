using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AlterarVencimentoCartao;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AlterarVencimentoCartao;

/// <summary>
/// Monta o <see cref="AlterarVencimentoCartaoRequest"/> com um <see cref="DiaVencimentoCartao"/> do fixture, que
/// sai diferente do vencimento de um cartão pedido no mesmo teste. Os ids ficam de fora: vêm da rota, que vence o
/// corpo.
/// </summary>
internal sealed class AlterarVencimentoCartaoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((DiaVencimentoCartao diaVencimento) => new AlterarVencimentoCartaoRequest
        {
            DiaVencimento = diaVencimento,
        });
    }
}
