using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>A importação da fatura de um cartão, com a data em que ela vence.</summary>
public sealed class ImportacaoFatura : ImportacaoArquivo
{
    public IdCartao IdCartao { get; private set; }

    /// <summary>O vencimento da fatura do arquivo.</summary>
    public VencimentoFatura Vencimento { get; private set; } = null!;

    /// <summary>O mês em que vence a fatura do arquivo, o do <see cref="Vencimento"/>.</summary>
    public MesReferencia MesVencimento => Vencimento.Mes;

    private ImportacaoFatura()
    {
    }

    private ImportacaoFatura(
        IdImportacao id,
        IdUsuario idUsuario,
        IdCartao idCartao,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        VencimentoFatura vencimento,
        int lidas,
        DateTimeOffset importadaEm)
            : base(id, idUsuario, layout, periodo, lidas, importadaEm)
    {
        IdCartao = idCartao;
        Vencimento = vencimento;
    }

    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <remarks>
    /// Interno pois o <paramref name="idUsuario"/> precisa ser o dono do cartão, e só a
    /// <see cref="Services.ImportacaoArquivoFactory"/>, que recebe o cartão, garante isso.
    /// </remarks>
    internal static ImportacaoFatura Registrar(
        IdUsuario idUsuario,
        IdCartao idCartao,
        LayoutArquivoImportacao layout,
        Periodo periodo,
        VencimentoFatura vencimento,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return new ImportacaoFatura(IdImportacao.FromNewVersion7Guid(), idUsuario, idCartao, layout, periodo, vencimento, lidas, importadaEm);
    }

    /// <summary>
    /// Se esta importação é de um cartão da <paramref name="fatura"/>, com vencimento no mesmo mês do dela, mas em outra
    /// data, pelo <see cref="VencimentoFatura.EhOutraDataNoMesmoMes"/>.
    /// </summary>
    public bool TemOutroVencimentoNoMesmoMes(FaturaCartao fatura) =>
        fatura.Contem(IdCartao) && Vencimento.EhOutraDataNoMesmoMes(fatura.Vencimento);

    /// <summary>
    /// O vencimento real da <paramref name="fatura"/>, quando esta importação a trouxe: é de um cartão dela, com vencimento
    /// no mês dela. É o que o arquivo trouxe ou o usuário informou, e não o projetado pelo dia do cartão. Vazio na
    /// importação de outra fatura.
    /// </summary>
    public Maybe<VencimentoFatura> VencimentoImportado(FaturaCartao fatura)
    {
        if (fatura.Contem(IdCartao) && MesVencimento == fatura.Vencimento.Mes)
            return Vencimento;

        return Maybe<VencimentoFatura>.None;
    }
}
