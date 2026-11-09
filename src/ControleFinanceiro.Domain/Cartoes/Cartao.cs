using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Cartoes;

[ValueObject]
public readonly partial struct IdCartao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

public sealed class Cartao : AggregateRoot<IdCartao>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }
    public Banco Banco { get; private set; } = null!;
    public SufixoCartao Sufixo { get; private set; } = null!;
    public DiaVencimentoCartao DiaVencimento { get; private set; } = null!;
    public DiaFechamentoCartao DiaFechamento { get; private set; } = null!;

    private Cartao()
    {
    }

    private Cartao(
        IdCartao id,
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
        : base(id)
    {
        IdUsuario = idUsuario;
        Banco = banco;
        Sufixo = sufixo;
        DiaVencimento = diaVencimento;
        DiaFechamento = diaFechamento;
    }

    /// <remarks>
    /// Interno pois o usuário não pode ter dois cartões do mesmo banco com o mesmo final, e só a
    /// <see cref="Services.CartaoFactory"/>, que recebe os cartões do usuário, confere isso.
    /// </remarks>
    internal static Cartao Criar(
        IdUsuario idUsuario,
        Banco banco,
        SufixoCartao sufixo,
        DiaVencimentoCartao diaVencimento,
        DiaFechamentoCartao diaFechamento)
    {
        return new Cartao(IdCartao.FromNewVersion7Guid(), idUsuario, banco, sufixo, diaVencimento, diaFechamento);
    }

    /// <remarks>
    /// As despesas já geradas não mudam, porque guardam a data projetada do pagamento: os dias novos valem para as
    /// geradas depois.
    /// </remarks>
    public void AlterarCicloFatura(DiaVencimentoCartao diaVencimento, DiaFechamentoCartao diaFechamento)
    {
        DiaVencimento = diaVencimento;
        DiaFechamento = diaFechamento;
    }

    public bool PossuiBancoESufixo(Banco banco, SufixoCartao sufixo) => Banco == banco && Sufixo == sufixo;

    /// <summary>
    /// Verdadeiro quando os dois cartões saem na mesma fatura: mesmo banco, mesmo dia de vencimento e mesmo dia de
    /// fechamento. No C6, cada final é um cartão, e os finais da mesma fatura são pagos juntos. Um cartão é da mesma
    /// fatura que ele mesmo.
    /// </summary>
    public bool EhDaMesmaFaturaQue(Cartao outro) =>
        Banco == outro.Banco && DiaVencimento == outro.DiaVencimento && DiaFechamento == outro.DiaFechamento;

    /// <summary>
    /// Liga a data de uma compra ao mês de vencimento da fatura em que ela cai. O dia de fechamento é o último dia de
    /// compras da fatura, e o vencimento cai no mês do fechamento só quando o dia de vencimento é maior que o de
    /// fechamento; com os dois dias iguais, vai para o mês seguinte.
    /// </summary>
    /// <remarks>
    /// O banco empurra o fechamento e o vencimento que caem em fim de semana ou feriado, e o cálculo não sabe disso: a
    /// compra feita nesses dias pode sair na fatura vizinha.
    /// </remarks>
    public MesReferencia MesVencimento(DateOnly dataCompra)
    {
        MesReferencia mesCompra = dataCompra;
        MesReferencia mesFechamento = dataCompra.Day <= DiaFechamento ? mesCompra : mesCompra.Posterior();

        return VenceNoMesDoFechamento ? mesFechamento : mesFechamento.Posterior();
    }

    /// <summary>O vencimento da fatura em que a compra cai: o <see cref="MesVencimento"/> dela, no dia de vencimento do cartão.</summary>
    public VencimentoFatura VencimentoDaCompra(DateOnly dataCompra) => new(MesVencimento(dataCompra), DiaVencimento);

    /// <summary>O vencimento da fatura do cartão no mês, no dia de vencimento dele.</summary>
    public VencimentoFatura VencimentoNoMes(MesReferencia mesVencimento) => new(mesVencimento, DiaVencimento);

    /// <summary>
    /// A inversa do <see cref="MesVencimento"/>: a data, no dia informado, da compra que cai na fatura que vence em
    /// <paramref name="mesVencimento"/>. Pode ficar até dois meses antes dele.
    /// </summary>
    public DateOnly DataOcorrenciaQueVenceEm(MesReferencia mesVencimento, DiaValidoEmTodoMes dia)
    {
        MesReferencia mesFechamento = VenceNoMesDoFechamento ? mesVencimento : mesVencimento.Anterior();
        MesReferencia mesCompra = dia <= DiaFechamento ? mesFechamento : mesFechamento.Anterior();

        return mesCompra.NoDia(dia);
    }

    private bool VenceNoMesDoFechamento => DiaVencimento > DiaFechamento;
}
