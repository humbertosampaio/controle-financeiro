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
/// Um lançamento importado: a <see cref="TransacaoConta"/>, do extrato de uma conta, ou a <see cref="TransacaoCartao"/>,
/// da fatura de um cartão. Data e hora ficam separadas: sem hora, um <see cref="DateTimeOffset"/> obrigaria a inventar
/// 00:00, e a conversão para UTC deslocaria o dia.
/// </summary>
/// <remarks>
/// A identidade é o <see cref="IdTransacao"/>, que os vínculos com despesas referenciam. A
/// <see cref="ChaveDeduplicacao"/> só reconhece o lançamento numa reimportação e muda com a versão do
/// algoritmo.
/// </remarks>
public abstract class Transacao : AggregateRoot<IdTransacao>, IPertenceAoUsuario
{
    public IdUsuario IdUsuario { get; private set; }

    /// <summary>
    /// A importação que criou a transação. Uma reimportação não a cria de novo, então ela continua ligada à
    /// primeira importação que a trouxe.
    /// </summary>
    public IdImportacao IdImportacao { get; private set; }

    public DateOnly Data { get; private set; }
    public TimeOnly? Hora { get; private set; }

    /// <summary>
    /// Com sinal, <b>do ponto de vista do usuário</b>: negativo é saída. O adaptador de uma fatura que traz as
    /// compras como positivas inverte o sinal ao ler.
    /// </summary>
    public Reais Valor { get; private set; } = null!;

    /// <summary>Verdadeiro quando o dinheiro saiu, ou seja, quando o <see cref="Valor"/> é negativo.</summary>
    public bool EhSaida => Valor < 0;

    /// <summary>
    /// Verdadeiro quando o dinheiro entrou, ou seja, quando o <see cref="Valor"/> é positivo. O valor zero não é
    /// entrada nem saída.
    /// </summary>
    public bool EhEntrada => Valor > 0;

    public TextoObrigatorio Descricao { get; private set; } = null!;
    public string? Contraparte { get; private set; }

    /// <summary>O <see cref="Valor"/> sem o sinal.</summary>
    public ReaisPositivo ValorAbsoluto => (ReaisPositivo)decimal.Abs(Valor);

    /// <summary>O texto do arquivo que diz quem recebeu o dinheiro de uma saída, pela regra do <see cref="Lancamento.TextoBeneficiario"/>.</summary>
    public abstract string? TextoBeneficiario { get; }

    /// <summary>O alias do <see cref="TextoBeneficiario"/>, pela regra do <see cref="Lancamento.AliasTextoBeneficiario"/>.</summary>
    public Alias? AliasTextoBeneficiario => Alias.DoTextoOuNulo(TextoBeneficiario);

    /// <summary>A categoria que o banco deu à compra, quando o layout traz.</summary>
    public string? CategoriaBanco { get; private set; }

    /// <summary>O alias da <see cref="CategoriaBanco"/>, pela regra do <see cref="Lancamento.AliasCategoriaBanco"/>.</summary>
    public Alias? AliasCategoriaBanco => Alias.DoTextoOuNulo(CategoriaBanco);

    public ChaveDeduplicacao Chave { get; private set; } = null!;

    protected Transacao()
    {
    }

    protected Transacao(
        IdTransacao id,
        IdUsuario idUsuario,
        IdImportacao idImportacao,
        Lancamento lancamento,
        ChaveDeduplicacao chave)
            : base(id)
    {
        IdUsuario = idUsuario;
        IdImportacao = idImportacao;
        Data = lancamento.Data;
        Hora = lancamento.Hora;
        Valor = lancamento.Valor;
        Descricao = lancamento.Descricao;
        Contraparte = lancamento.Contraparte;
        CategoriaBanco = lancamento.CategoriaBanco;
        Chave = chave;
    }
}
