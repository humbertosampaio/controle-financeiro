using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Transacoes;

namespace ControleFinanceiro.Application.SaidaModelo;

public static class ParaModeloExtensions
{
    extension(Transacao transacao)
    {
        public TransacaoParaModelo ParaModelo() => TransacaoParaModelo.De(transacao);
    }

    extension(Despesa despesa)
    {
        public DespesaParaModelo ParaModelo() => DespesaParaModelo.De(despesa);
    }

    extension(Beneficiario beneficiario)
    {
        public BeneficiarioParaModelo ParaModelo() => BeneficiarioParaModelo.De(beneficiario);
    }
}
