using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Transacoes;

[ValueObject]
public readonly partial struct IdTransacao
{
    private static Validation Validate(Guid input) => Id.Validar(input);
}

/// <summary>
/// Um lançamento importado de um extrato ou de uma fatura. Data e hora ficam separadas: sem hora, um
/// <see cref="DateTimeOffset"/> obrigaria a inventar 00:00, e a conversão para UTC deslocaria o dia.
/// </summary>
/// <remarks>
/// A identidade é o <see cref="IdTransacao"/>, que os vínculos com despesas referenciam. A
/// <see cref="ChaveDeduplicacao"/> só reconhece o lançamento numa reimportação e muda com a versão do
/// algoritmo.
/// </remarks>
public sealed class Transacao : AggregateRoot<IdTransacao>
{
    public IdUsuario IdUsuario { get; private set; }

    /// <summary>
    /// A importação que criou a transação. Uma reimportação não a cria de novo, então ela continua ligada à
    /// primeira importação que a trouxe.
    /// </summary>
    public IdImportacao IdImportacao { get; private set; }

    public OrigemTransacao Origem { get; private set; } = null!;
    public DateOnly Data { get; private set; }
    public TimeOnly? Hora { get; private set; }

    /// <summary>
    /// Com sinal, <b>do ponto de vista do usuário</b>: negativo é saída. O adaptador de uma fatura que traz as
    /// compras como positivas inverte o sinal ao ler.
    /// </summary>
    public Reais Valor { get; private set; } = null!;

    public TextoObrigatorio Descricao { get; private set; } = null!;
    public string? Contraparte { get; private set; }
    public ChaveDeduplicacao Chave { get; private set; } = null!;

    private Transacao()
    {
    }

    private Transacao(
        IdTransacao id,
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        OrigemTransacao origem,
        LancamentoExtrato lancamento,
        ChaveDeduplicacao chave)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdImportacao = idImportacao;
        Origem = origem;
        Data = lancamento.Data;
        Hora = lancamento.Hora;
        Valor = lancamento.Valor;
        Descricao = lancamento.Descricao;
        Contraparte = lancamento.Contraparte;
        Chave = chave;
    }

    /// <remarks>
    /// Interno pois a <paramref name="chave"/> precisa vir de <see cref="ChaveDeduplicacao.CalcularLote"/>, com a mesma origem e o lote do
    /// lançamento, o <paramref name="idUsuario"/> precisa ser o dono da origem e a importação precisa ser da mesma origem, e só a
    /// <see cref="Services.LoteTransacoesFactory"/> garante isso.
    /// </remarks>
    internal static Transacao Criar(
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        OrigemTransacao origem,
        LancamentoExtrato lancamento,
        ChaveDeduplicacao chave)
    {
        return new Transacao(IdTransacao.FromNewVersion7Guid(), idUsuario, idImportacao, origem, lancamento, chave);
    }
}
