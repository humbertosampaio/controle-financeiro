using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.Attributes;

/// <summary>
/// Monta a <see cref="ImportacaoArquivo"/> como a importação da fatura de um cartão (<see cref="ImportacaoFaturaGenerator"/>),
/// e não do extrato de uma conta, e o <see cref="VencimentoFatura"/> no dia de vencimento de um cartão num mês. Com
/// <c>[Frozen] Cartao</c> no teste, a importação sai desse cartão; com <c>[Frozen] MesReferencia</c>, todo vencimento cai
/// nesse mês, cada um num dia.
/// </summary>
public sealed class ImportacaoFaturaAutoDataAttribute : DomainAutoDataAttribute
{
    public ImportacaoFaturaAutoDataAttribute()
        : base(fixture =>
        {
            fixture.Customizations.Insert(index: 0, new ImportacaoFaturaGenerator());
            fixture.Register((MesReferencia mes, DiaVencimentoCartao dia) => VencimentoFatura.Projetado(mes, dia));
        })
    {
    }
}
