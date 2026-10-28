using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaAdiada : DespesaInteira
{
    public IdCartao IdCartao { get; private set; }

    private DespesaAdiada(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        FormaPagamento formaPagamento,
        IdCartao idCartao,
        IdDespesaRecorrente? idDespesaRecorrente,
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
            idDespesaRecorrente,
            mesReferencia,
            pagamento)
    {
        IdCartao = idCartao;
    }

    /// <param name="idDespesaRecorrente">A recorrente que gerou a despesa, ou nulo quando ela não veio de uma.</param>
    public static DespesaAdiada Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        IdCartao idCartao,
        DiaVencimentoCartao diaVencimento,
        IdDespesaRecorrente? idDespesaRecorrente)
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
            idCartao,
            idDespesaRecorrente,
            mesReferenciaPagamento,
            pagamento);
    }
}
