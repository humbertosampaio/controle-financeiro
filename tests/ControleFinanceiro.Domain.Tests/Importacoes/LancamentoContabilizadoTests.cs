using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

namespace ControleFinanceiro.Domain.Tests.Importacoes;

public sealed class LancamentoContabilizadoTests
{
    [Theory, DomainAutoData]
    public void Equals_Quando_MesmosCampos_Deve_SerIgual(Lancamento lancamento, DateOnly dataContabil)
    {
        LancamentoContabilizado contabilizado = LancamentoContabilizado.Criar(lancamento, dataContabil);

        LancamentoContabilizado outro = LancamentoContabilizado.Criar(lancamento, dataContabil);

        outro.Should().Be(contabilizado);
    }
}
