using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

namespace ControleFinanceiro.WebApi.Tests.Usuarios.AdicionarCartao;

/// <summary>
/// Monta o <see cref="AdicionarCartaoRequest"/> a partir de um <see cref="Cartao"/> do fixture. Com
/// <c>[Frozen] Cartao</c> no teste, a requisição sai do mesmo cartão que o teste espera encontrar no banco.
/// O <see cref="AdicionarCartaoRequest.IdUsuario"/> fica de fora: vem da rota, que vence o corpo.
/// </summary>
internal sealed class AdicionarCartaoRequestCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Register((Cartao cartao) => new AdicionarCartaoRequest
        {
            Banco = cartao.Banco,
            Sufixo = cartao.Sufixo,
            DiaVencimento = cartao.DiaVencimento,
        });
    }
}
