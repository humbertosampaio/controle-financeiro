using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// A importação do extrato de uma conta. É por ela que a importação seguinte da conta descobre se ficou um buraco entre
/// as duas.
/// </summary>
public sealed class ImportacaoExtrato : ImportacaoArquivo
{
    public IdConta IdConta { get; private set; }

    private ImportacaoExtrato()
    {
    }

    private ImportacaoExtrato(
        IdImportacao id,
        IdUsuario idUsuario,
        IdConta idConta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        int lidas,
        DateTimeOffset importadaEm)
            : base(id, idUsuario, layout, periodo, lidas, importadaEm)
    {
        IdConta = idConta;
    }

    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <remarks>
    /// Interno pois o <paramref name="idUsuario"/> precisa ser o dono da conta, e só a
    /// <see cref="Services.ImportacaoArquivoFactory"/>, que recebe a conta, garante isso.
    /// </remarks>
    internal static ImportacaoExtrato Registrar(
        IdUsuario idUsuario,
        IdConta idConta,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return new ImportacaoExtrato(IdImportacao.FromNewVersion7Guid(), idUsuario, idConta, layout, periodo, lidas, importadaEm);
    }

    /// <summary>
    /// Os dias entre o fim da <paramref name="ultima"/> importação da conta e o início desta, que nenhum
    /// arquivo cobriu. Nada quando é a primeira importação ou quando os períodos se tocam ou se sobrepõem.
    /// </summary>
    /// <param name="ultima">A importação mais recente da mesma conta, antes desta.</param>
    /// <exception cref="ArgumentException">Quando a importação anterior é de outra conta.</exception>
    public Maybe<Periodo> PeriodoNaoImportado(Maybe<ImportacaoExtrato> ultima)
    {
        if (ultima.HasNoValue)
            return Maybe.None;

        if (ultima.Value.IdConta != IdConta)
            throw new ArgumentException("A importação anterior é de outra conta.", nameof(ultima));

        // O período entre as duas só existe quando sobra pelo menos um dia: com os períodos se tocando ou se
        // sobrepondo, o início fica depois do fim, o Criar falha, e não há período não importado.
        return Periodo
            .Criar(ultima.Value.Periodo.Fim.AddDays(1), Periodo.Inicio.AddDays(-1))
            .AsMaybe();
    }
}
