using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Despesas;

public sealed class DespesaParcelada : Despesa
{
    private readonly List<ParcelaDespesa> _parcelas = [];

    public IdCartao IdCartao { get; private set; }
    public IReadOnlyList<ParcelaDespesa> Parcelas => _parcelas.AsReadOnly();

    private DespesaParcelada()
    {
    }

    private DespesaParcelada(
        IdDespesa id,
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        IdCartao idCartao)
        : base(
            id,
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            FormaPagamento.Credito,
            idDespesaRecorrente: null)
    {
        IdCartao = idCartao;
    }

    /// <summary>
    /// A compra com todas as parcelas, em meses consecutivos e com o mesmo valor. As anteriores à informada nascem
    /// pagas, e ela e as seguintes, pendentes.
    /// </summary>
    /// <remarks>
    /// Interno porque só a <see cref="Services.DespesaParceladaFactory"/>, que recebe as listas do usuário, recusa a
    /// compra repetida, confere a posse dos itens e calcula o mês da primeira parcela pelos dias do cartão.
    /// </remarks>
    /// <param name="valor">O valor da parcela informada, o único que o arquivo traz.</param>
    /// <param name="mesPrimeiraParcela">O mês de vencimento da fatura em que a primeira parcela cai.</param>
    /// <param name="parcela">A parcela como o banco a informou.</param>
    internal static DespesaParcelada Criar(
        IdUsuario idUsuario,
        IdBeneficiario idBeneficiario,
        TextoObrigatorio descricao,
        IdCategoriaDespesa? idCategoria,
        DateOnly dataOcorrencia,
        ReaisPositivo valor,
        IdCartao idCartao,
        MesReferencia mesPrimeiraParcela,
        DiaVencimentoCartao diaVencimento,
        Parcela parcela)
    {
        DespesaParcelada despesa = new(
            IdDespesa.FromNewVersion7Guid(),
            idUsuario,
            idBeneficiario,
            descricao,
            idCategoria,
            dataOcorrencia,
            idCartao);

        MesReferencia mes = mesPrimeiraParcela;
        for (int numero = 1; numero <= parcela.Total; numero++)
        {
            despesa._parcelas.Add(numero < parcela.Numero
                ? ParcelaDespesa.CriarPaga(numero, mes, diaVencimento, valor)
                : ParcelaDespesa.CriarPendente(numero, mes, diaVencimento, valor));

            mes = mes.Posterior();
        }

        return despesa;
    }

    public Maybe<ParcelaDespesa> ParcelaDoMes(MesReferencia mesReferencia) => _parcelas.TryFirst(p => p.MesReferencia == mesReferencia);

    public bool EhDaCompra(ChaveCompraParcelada chave) =>
        chave == new ChaveCompraParcelada(IdCartao, DataOcorrencia, (TextoNormalizado)Descricao, _parcelas.Count);

    /// <summary>Interno porque só o <see cref="Services.VinculoPagamentoService"/> confere que a transação é desta compra.</summary>
    /// <param name="parcela">A parcela como o banco a informou, de uma compra com o mesmo total.</param>
    internal UnitResult<Erro> QuitarParcela(Parcela parcela, IdTransacao idTransacao, ReaisPositivo valorEfetivo) =>
        _parcelas.First(p => p.NumeroParcela == parcela.Numero).QuitarCom(idTransacao, valorEfetivo);

    public override UnitResult<Erro> DesfazerVinculo(IdTransacao idTransacao)
    {
        ParcelaDespesa? parcela = _parcelas.Find(p => p.Pagamento.IdTransacao == idTransacao);
        if (parcela is null)
            return ErroTransacaoNaoVinculada.Instancia;

        parcela.Reabrir();
        return UnitResult.Success<Erro>();
    }
}
