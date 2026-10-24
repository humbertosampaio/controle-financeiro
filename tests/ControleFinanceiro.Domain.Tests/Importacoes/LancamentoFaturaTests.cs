using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class LancamentoFaturaTests
{
    [Theory, DomainAutoData]
    public void Criar_Deve_GuardarOLancamentoEOFinal(Lancamento lancamento, SufixoCartao sufixo)
    {
        LancamentoFatura result = LancamentoFatura.Criar(lancamento, sufixo);

        result.Lancamento.Should().Be(lancamento);
        result.SufixoCartao.Should().Be(sufixo);
    }

    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(Lancamento lancamento, SufixoCartao sufixo)
    {
        LancamentoFatura lancamentoFatura = LancamentoFatura.Criar(lancamento, sufixo);

        LancamentoFatura outro = LancamentoFatura.Criar(lancamento, sufixo);

        outro.Should().Be(lancamentoFatura);
    }
}
