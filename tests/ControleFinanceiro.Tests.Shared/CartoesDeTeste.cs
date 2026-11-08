using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;

namespace ControleFinanceiro.Tests.Shared;

/// <summary>
/// Outro cartão do mesmo usuário, montado pela <see cref="CartaoFactory"/> a partir de um cartão do fixture, trocando só
/// o que o teste escolhe. Para outros dias, o teste chama o <see cref="Cartao.AlterarCicloFatura"/> no cartão devolvido.
/// </summary>
public static class CartoesDeTeste
{
    extension(Cartao cartao)
    {
        /// <summary>Outro final do mesmo banco, com os mesmos dias de vencimento e de fechamento: sai na mesma fatura.</summary>
        public Cartao OutroFinal(SufixoCartao sufixo) => cartao.Copiar(cartao.Banco, sufixo);

        /// <summary>O mesmo final e os mesmos dias, num banco diferente do dele.</summary>
        public Cartao EmOutroBanco() => cartao.Copiar(Banco.List.First(b => b != cartao.Banco), cartao.Sufixo);

        private Cartao Copiar(Banco banco, SufixoCartao sufixo)
        {
            return CartaoFactory.Criar(cartao.IdUsuario, cartoesDoUsuario: [], banco, sufixo, cartao.DiaVencimento, cartao.DiaFechamento).Value;
        }
    }
}
