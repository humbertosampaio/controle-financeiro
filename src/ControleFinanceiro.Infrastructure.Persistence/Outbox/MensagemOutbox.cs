namespace ControleFinanceiro.Infrastructure.Persistence.Outbox;

/// <summary>
/// Uma mensagem gravada na mesma transação do agregado que a originou, para ser processada depois por outro processo.
/// É infraestrutura, e não conceito do domínio: o id é um <see cref="Guid"/> v7, sem Vogen.
/// </summary>
public sealed class MensagemOutbox
{
    public Guid Id { get; private set; }

    /// <summary>O nome do tipo do conteúdo, pelo qual ele é desserializado.</summary>
    public string Tipo { get; private set; } = null!;

    /// <summary>O conteúdo em JSON.</summary>
    public string Conteudo { get; private set; } = null!;

    public DateTimeOffset RegistradaEm { get; private set; }

    /// <summary>Quando foi processada; nula enquanto pendente.</summary>
    public DateTimeOffset? ProcessadaEm { get; private set; }

    /// <summary>Quantas vezes o processamento falhou.</summary>
    public int Tentativas { get; private set; }

    /// <summary>O erro da última tentativa que falhou.</summary>
    public string? UltimoErro { get; private set; }

    private MensagemOutbox()
    {
    }

    internal static MensagemOutbox Registrar(string tipo, string conteudo, DateTimeOffset registradaEm)
    {
        return new MensagemOutbox
        {
            Id = Guid.CreateVersion7(),
            Tipo = tipo,
            Conteudo = conteudo,
            RegistradaEm = registradaEm,
        };
    }
}
