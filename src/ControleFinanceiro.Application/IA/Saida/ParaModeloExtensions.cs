using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.IA.Saida;

public static class ParaModeloExtensions
{
    extension(Transacao transacao)
    {
        public TransacaoParaModelo ParaModelo() => TransacaoParaModelo.De(transacao);
    }

    extension(Despesa despesa)
    {
        public DespesaParaModelo ParaModelo(MesReferencia mes) => DespesaParaModelo.De(despesa, mes);
    }

    extension(DespesaRecorrente recorrente)
    {
        public DespesaFixaParaModelo ParaModelo(Despesa? despesaDoMes, MesReferencia mes) => DespesaFixaParaModelo.De(recorrente, despesaDoMes, mes);
    }

    extension(Beneficiario beneficiario)
    {
        public BeneficiarioParaModelo ParaModelo() => BeneficiarioParaModelo.De(beneficiario);
    }

    extension(BalancoMensal balanco)
    {
        public BalancoMensalParaModelo ParaModelo() => BalancoMensalParaModelo.De(balanco);
    }
}
