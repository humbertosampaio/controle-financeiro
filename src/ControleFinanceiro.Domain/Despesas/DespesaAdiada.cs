using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaAdiada : DespesaInteira
{
    private DespesaAdiada(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        MesReferencia mesReferencia,
        Pagamento pagamento)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            formaPagamento,
            mesReferencia,
            pagamento)
    {
    }

    public static DespesaAdiada Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        DiaVencimentoCartao diaVencimento)
    {
        MesReferencia mesReferenciaOcorrencia = dataOcorrencia;
        MesReferencia mesReferenciaPagamento = mesReferenciaOcorrencia.Posterior();
        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada: mesReferenciaPagamento.NoDia(diaVencimento));

        return new DespesaAdiada(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito,
            mesReferenciaPagamento,
            pagamento);
    }
}
