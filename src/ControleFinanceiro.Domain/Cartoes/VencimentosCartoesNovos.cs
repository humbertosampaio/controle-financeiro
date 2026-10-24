namespace ControleFinanceiro.Domain.Cartoes;

/// <summary>
/// Os dias de vencimento dos cartões que uma importação vai criar, informados pelo usuário junto com o arquivo.
/// Para cada final vale o dia do próprio final, senão o dia único, senão o <see cref="DiaVencimentoCartao.Padrao"/>.
/// </summary>
/// <remarks>
/// Só serve aos cartões novos: o cartão que já existe mantém o próprio vencimento. Um final informado que não
/// aparece no arquivo é ignorado.
/// </remarks>
public sealed class VencimentosCartoesNovos : ValueObject
{
    private readonly DiaVencimentoCartao? _diaUnico;
    private readonly Dictionary<SufixoCartao, DiaVencimentoCartao> _porSufixo;

    /// <remarks>
    /// Os finais ficam em ordem, para a comparação não depender da ordem em que o usuário os informou.
    /// </remarks>
    private VencimentosCartoesNovos(DiaVencimentoCartao? diaUnico, IEnumerable<KeyValuePair<SufixoCartao, DiaVencimentoCartao>> porSufixo)
    {
        _diaUnico = diaUnico;
        _porSufixo = porSufixo.OrderBy(p => p.Key).ToDictionary();
    }

    /// <summary>Falha quando um final aparece mais de uma vez em <paramref name="porSufixo"/>.</summary>
    /// <param name="diaUnico">O dia dos cartões novos cujo final não está em <paramref name="porSufixo"/>.</param>
    /// <param name="porSufixo">O dia de cada final.</param>
    public static Result<VencimentosCartoesNovos> Criar(
        DiaVencimentoCartao? diaUnico,
        IEnumerable<KeyValuePair<SufixoCartao, DiaVencimentoCartao>> porSufixo)
    {
        List<KeyValuePair<SufixoCartao, DiaVencimentoCartao>> dias = porSufixo.ToList();

        // O construtor monta um dicionário, que não aceita o final repetido: ele só roda depois da conferência.
        return Result
            .SuccessIf(dias.DistinctBy(p => p.Key).Count() == dias.Count, "Os vencimentos não podem repetir o final do cartão.")
            .Map(() => new VencimentosCartoesNovos(diaUnico, dias));
    }

    public DiaVencimentoCartao Para(SufixoCartao sufixo) => _porSufixo.GetValueOrDefault(sufixo) ?? _diaUnico ?? DiaVencimentoCartao.Padrao;

    /// <summary>Verdadeiro quando o usuário não informou o dia do final, nem um dia único.</summary>
    public bool EhPadrao(SufixoCartao sufixo) => !_porSufixo.ContainsKey(sufixo) && _diaUnico is null;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return _diaUnico;

        foreach (KeyValuePair<SufixoCartao, DiaVencimentoCartao> diaDoSufixo in _porSufixo)
            yield return diaDoSufixo;
    }
}
