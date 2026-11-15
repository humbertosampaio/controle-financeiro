using AutoFixture.Kernel;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Tests.Shared.AutoFixture.SpecimenBuilders;

/// <summary>
/// Monta a <see cref="FaturaCartaoAberta"/> pelo <see cref="CandidatosPagamentoFaturaService"/>, a única entrada: a
/// fatura de 100,00 de um cartão, aberta pelo candidato que uma saída da conta de 100,00 no dia do vencimento forma. O
/// cartão e a conta saem do mesmo usuário. Com <c>[Frozen] VencimentoFatura</c> no teste, a fatura vence nessa data.
/// </summary>
public sealed class FaturaCartaoAbertaGenerator : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is not Type t || t != typeof(FaturaCartaoAberta))
            return new NoSpecimen();

        IdUsuario idUsuario = context.Create<IdUsuario>();
        Cartao cartao = CartaoFactory
            .Criar(
                idUsuario,
                cartoesDoUsuario: [],
                context.Create<Banco>(),
                context.Create<SufixoCartao>(),
                context.Create<DiaVencimentoCartao>(),
                context.Create<DiaFechamentoCartao>())
            .Value;
        ContaBancaria conta = ContaBancariaFactory
            .ObterOuCriar(idUsuario, contasDoUsuario: [], context.Create<Banco>(), context.Create<TipoConta>(), context.Create<SufixoConta>())
            .Value;
        CenarioPagamentoFatura cenario = new(
            idUsuario,
            conta,
            context.Create<LayoutArquivoImportacao>(),
            context.Create<Periodo>(),
            context.Create<DateTimeOffset>());
        VencimentoFatura vencimento = context.Create<VencimentoFatura>();

        cenario.ImportarFatura(cartao, vencimento, context.Create<Lancamento>().NoCartaoComValor(-100m));
        cenario.Saida(context.Create<Lancamento>(), vencimento, (ReaisPositivo)100m);
        return cenario.Apontar([cartao], vencimento.Mes).Single().FaturaCartaoAberta;
    }
}
