using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaAdiada : DespesaInteira
{
    public IdCartao IdCartao { get; private set; }

    private DespesaAdiada()
    {
    }

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
            pagamento)
    {
        IdCartao = idCartao;
    }

    /// <param name="mesVencimento">O mês de vencimento da fatura em que a ocorrência cai.</param>
    /// <param name="idDespesaRecorrente">A recorrente que gerou a despesa, ou nulo quando ela não veio de uma.</param>
    /// <exception cref="ArgumentException">Quando o mês de vencimento vem antes do mês da ocorrência.</exception>
    public static DespesaAdiada Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        IdCartao idCartao,
        MesReferencia mesVencimento,
        DiaVencimentoCartao diaVencimento,
        IdDespesaRecorrente? idDespesaRecorrente)
    {
        MesReferencia mesOcorrencia = dataOcorrencia;
        if (mesVencimento < mesOcorrencia)
            throw new ArgumentException("O mês de vencimento vem antes do mês da ocorrência.", nameof(mesVencimento));

        Pagamento pagamento = Pagamento.Pendente(valor, dataProjetada: mesVencimento.NoDia(diaVencimento));

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
            pagamento);
    }

    /// <summary>
    /// Interno porque só o <see cref="Services.VinculoPagamentoService"/> confere a transação. A data efetiva é o vencimento
    /// projetado, porque a data da transação é a da compra, e não a do pagamento.
    /// </summary>
    internal UnitResult<Erro> QuitarCom(IdTransacao idTransacao, ReaisPositivo valorEfetivo) => Quitar(idTransacao, Pagamento.DataProjetada, valorEfetivo);
}
