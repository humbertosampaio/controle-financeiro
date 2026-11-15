using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;

namespace ControleFinanceiro.Domain.Services;

/// <summary>
/// Registra a importação de um arquivo da conta ou do cartão. O usuário e a origem saem da própria conta ou
/// do próprio cartão, para a importação nunca ficar com o dono de um e a origem de outro.
/// </summary>
public static class ImportacaoArquivoFactory
{
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    public static ImportacaoArquivo Registrar(
        ContaBancaria conta,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ImportacaoArquivo.RegistrarExtrato(conta.IdUsuario, OrigemTransacao.DaConta(conta.Id), layout, periodo, lidas, importadaEm);
    }

    /// <summary>
    /// Registra a importação da fatura do <paramref name="cartao"/>, depois de conferir pela
    /// <see cref="ConferirVencimentoFatura"/> que a fatura não foi importada com outro vencimento no mesmo mês.
    /// </summary>
    /// <param name="cartoesUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <param name="importacoesFaturaMes">As importações de fatura do usuário com vencimento no mês do <paramref name="vencimento"/>.</param>
    /// <param name="vencimento">O vencimento da fatura do arquivo.</param>
    /// <param name="lidas">Quantos lançamentos o arquivo trazia.</param>
    /// <exception cref="ArgumentException">Quando um cartão ou uma importação é de outro usuário.</exception>
    public static Result<ImportacaoArquivo, Erro> Registrar(
        Cartao cartao,
        IReadOnlyCollection<Cartao> cartoesUsuario,
        IReadOnlyCollection<ImportacaoArquivo> importacoesFaturaMes,
        LayoutArquivoImportacao layout,
        PeriodoArquivo periodo,
        VencimentoFatura vencimento,
        int lidas,
        DateTimeOffset importadaEm)
    {
        return ConferirVencimentoFatura(cartao.IdUsuario, vencimento, cartoesFatura: [cartao], cartoesUsuario, importacoesFaturaMes)
            .Map(() => ImportacaoArquivo.RegistrarFatura(
                cartao.IdUsuario,
                OrigemTransacao.DoCartao(cartao.Id),
                layout,
                periodo,
                vencimento,
                lidas,
                importadaEm));
    }

    /// <summary>
    /// Confere que a fatura tem um vencimento só: falha com <see cref="ErroFaturaJaImportadaComOutroVencimento"/> quando um
    /// cartão da mesma fatura (<see cref="Cartao.EhDaMesmaFaturaQue"/>) de algum dos <paramref name="cartoesFatura"/> já
    /// tem uma importação com vencimento no mesmo mês, mas em outra data. O mês é conferido de novo aqui, sem confiar no
    /// filtro de quem montou a lista.
    /// </summary>
    /// <remarks>
    /// Sem índice único: a regra depende do ciclo atual dos cartões, que não é coluna da importação, e duas importações
    /// ao mesmo tempo podem passar.
    /// </remarks>
    /// <param name="cartoesFatura">Os cartões dos finais do arquivo.</param>
    /// <param name="cartoesUsuario">Todos os cartões do usuário, e só os dele.</param>
    /// <param name="importacoesFaturaMes">As importações de fatura do usuário com vencimento no mês do <paramref name="vencimento"/>.</param>
    /// <exception cref="ArgumentException">Quando um cartão ou uma importação é de outro usuário.</exception>
    public static UnitResult<Erro> ConferirVencimentoFatura(
        IdUsuario idUsuario,
        VencimentoFatura vencimento,
        IReadOnlyCollection<Cartao> cartoesFatura,
        IReadOnlyCollection<Cartao> cartoesUsuario,
        IReadOnlyCollection<ImportacaoArquivo> importacoesFaturaMes)
    {
        if (cartoesFatura.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesFatura));

        if (cartoesUsuario.Any(c => c.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz um cartão de outro usuário.", nameof(cartoesUsuario));

        if (importacoesFaturaMes.Any(i => i.IdUsuario != idUsuario))
            throw new ArgumentException("A lista traz uma importação de outro usuário.", nameof(importacoesFaturaMes));

        FaturaCartao fatura = FaturaCartao.Importada(cartoesFatura, cartoesUsuario, vencimento);

        return UnitResult.FailureIf<Erro>(
            importacoesFaturaMes.Any(i => i.TemOutroVencimentoNoMesmoMes(fatura)),
            ErroFaturaJaImportadaComOutroVencimento.Instancia);
    }
}
