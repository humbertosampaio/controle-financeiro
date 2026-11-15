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
    /// <summary>
    /// Quantos dias corridos depois do dia de vencimento o banco pode empurrar o vencimento que cai em fim de semana ou
    /// feriado: cobre o fim de semana emendado num feriado.
    /// </summary>
    public const int ToleranciaDiasVencimento = 4;

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
    /// Confere o vencimento da fatura com o ciclo cadastrado: ele cai do dia de vencimento do cartão, no mês dele, até
    /// <see cref="ToleranciaDiasVencimento"/> dias corridos depois, como o banco faz com o vencimento que cai em fim de
    /// semana ou feriado. Falha com um aviso de texto fixo, sem dado do usuário, que não impede a importação.
    /// </summary>
    public Result ConferirVencimento(DateOnly vencimento)
    {
        DateOnly cadastrado = VencimentoNoMes(vencimento);

        return Result.SuccessIf(
            vencimento >= cadastrado && vencimento <= cadastrado.AddDays(ToleranciaDiasVencimento),
            "O vencimento da fatura não fecha com o dia de vencimento cadastrado no cartão: confira a data do vencimento e o ciclo do cartão.");
    }

    /// <summary>O vencimento da fatura do cartão no mês, no dia de vencimento dele.</summary>
    public VencimentoFatura VencimentoNoMes(MesReferencia mesVencimento) => VencimentoFatura.Projetado(mesVencimento, DiaVencimento);

    /// <summary>
    /// A data, no dia informado, da compra que cai na fatura que vence em <paramref name="mesVencimento"/>, pelos dias do
    /// cartão: o dia de fechamento é o último dia de compras da fatura, e o vencimento cai no mês do fechamento só quando o
    /// dia de vencimento é maior que o de fechamento; com os dois dias iguais, vai para o mês seguinte. Pode ficar até dois
    /// meses antes dele. Serve à previsão do que ainda não veio em arquivo: a compra importada é da fatura que a trouxe.
    /// </summary>
    /// <remarks>
    /// O banco empurra o fechamento que cai em fim de semana ou feriado, e o cálculo não sabe disso: a compra prevista
    /// para esses dias pode sair na fatura vizinha.
    /// </remarks>
    public DateOnly DataOcorrenciaQueVenceEm(MesReferencia mesVencimento, DiaValidoEmTodoMes dia)
    {
        MesReferencia mesFechamento = VenceNoMesDoFechamento ? mesVencimento : mesVencimento.Anterior();
        MesReferencia mesCompra = dia <= DiaFechamento ? mesFechamento : mesFechamento.Anterior();

        return mesCompra.NoDia(dia);
    }

    private bool VenceNoMesDoFechamento => DiaVencimento > DiaFechamento;
}
