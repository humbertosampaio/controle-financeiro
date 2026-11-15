using ControleFinanceiro.Domain.Shared;

namespace ControleFinanceiro.Domain.Importacoes;

/// <summary>
/// Um cartão da mesma fatura já tem uma importação com vencimento no mesmo mês, mas em outra data, pela conferência da
/// <see cref="Services.ImportacaoArquivoFactory.ConferirVencimentoFatura"/>: cada fatura tem um vencimento só.
/// </summary>
public sealed class ErroFaturaJaImportadaComOutroVencimento : Erro
{
    public static readonly ErroFaturaJaImportadaComOutroVencimento Instancia = new();

    internal ErroFaturaJaImportadaComOutroVencimento()
        : base(
            "Importacoes.FaturaJaImportadaComOutroVencimento",
            "A fatura já foi importada com outro vencimento no mesmo mês, e cada fatura tem um vencimento só: confira a data do vencimento.")
    {
    }
}
