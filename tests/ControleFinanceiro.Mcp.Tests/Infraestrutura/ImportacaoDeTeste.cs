using ControleFinanceiro.Application.Importacoes;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Cartoes;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Infrastructure.Importacao.C6;
using ControleFinanceiro.Infrastructure.Importacao.MercadoPago;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Tests.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Monta no banco, para o usuário do servidor, os cadastros que as fixtures fictícias dos leitores pedem: os cartões
/// dos finais da fatura, os beneficiários e as categorias, com o próprio texto do arquivo como nome e alias. O arquivo é
/// lido aqui pelo leitor de verdade, como o servidor o lê. Conta também o que ficou gravado, para os testes lerem de
/// volta do banco.
/// </summary>
internal sealed class ImportacaoDeTeste
{
    public const string ExtratoC6 = "dados/C6/extrato-conta.csv";
    public const string ExtratoMercadoPago = "dados/MercadoPago/extrato-conta.csv";
    public const string FaturaC6 = "dados/C6/fatura-cartao.csv";
    public const string FaturaMercadoPago = "dados/MercadoPago/fatura-cartao.csv";

    /// <summary>O sufixo que o usuário informa para o extrato do Mercado Pago, que não traz a conta.</summary>
    public const string SufixoContaMercadoPago = "5678";

    private static readonly DiaVencimentoCartao _diaVencimento = (DiaVencimentoCartao)10;
    private static readonly DiaFechamentoCartao _diaFechamento = (DiaFechamentoCartao)3;

    private readonly ServidorMcpFixture _servidor;

    public ImportacaoDeTeste(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
    }

    /// <summary>O caminho completo de um arquivo da pasta de origem, pelo caminho relativo que a ferramenta recebe.</summary>
    public static string Completo(string caminho) => Path.Combine(ServidorMcpFixture.RepositorioDeTeste, caminho);

    /// <summary>O número inteiro da conta, como o cabeçalho do extrato fictício do C6 o traz.</summary>
    public static string NumeroContaDoCabecalhoC6()
    {
        string conteudo = File.ReadAllText(Completo(ExtratoC6), Encoding.UTF8);
        return Regex.Match(conteudo, @"Conta:\s*(?<numero>\S+)").Groups["numero"].Value;
    }

    /// <summary>Os lançamentos do arquivo, lidos pelo leitor do layout.</summary>
    public static async Task<IReadOnlyList<Lancamento>> LerLancamentosAsync(LayoutArquivoImportacao layout, string caminho)
    {
        if (LayoutArquivoImportacao.Faturas.Contains(layout))
            return (await LerFaturaAsync(layout, caminho)).Lancamentos.Select(lancamento => lancamento.Lancamento).ToArray();

        return (await LerExtratoAsync(layout, caminho)).Lancamentos;
    }

    public static async Task<Fatura> LerFaturaAsync(LayoutArquivoImportacao layout, string caminho)
    {
        await using FileStream arquivo = File.OpenRead(Completo(caminho));
        ILeitorFatura leitor = layout == LayoutArquivoImportacao.C6CartaoCsv ? new LeitorFaturaCartaoC6() : new LeitorFaturaCartaoMercadoPago();
        return (await leitor.LerAsync(arquivo, CancellationToken.None)).Value;
    }

    /// <summary>Cadastra tudo o que o arquivo pede para ser importado: os cartões, os beneficiários e as categorias.</summary>
    public async Task ResolverAsync(LayoutArquivoImportacao layout, string caminho)
    {
        await ResolverAliasesAsync(layout, caminho);

        if (LayoutArquivoImportacao.Faturas.Contains(layout))
            await CadastrarCartoesAsync(await LerFaturaAsync(layout, caminho));
    }

    /// <summary>Cadastra os beneficiários e as categorias do arquivo, mas não os cartões.</summary>
    public async Task ResolverAliasesAsync(LayoutArquivoImportacao layout, string caminho)
    {
        IReadOnlyList<Lancamento> lancamentos = await LerLancamentosAsync(layout, caminho);
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(_servidor.Usuario.Id, lancamentos);
        IReadOnlyList<CategoriaDespesa> categorias = CategoriasResolvidas(lancamentos);

        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(_servidor.Usuario.Id);
        await dbContext.Beneficiarios.AddRangeAsync(beneficiarios);
        await dbContext.Categorias.AddRangeAsync(categorias);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Grava na pasta de origem o arquivo do <paramref name="caminhoBase"/> com uma linha trocada, num nome só dele.
    /// </summary>
    /// <param name="numero">O número da linha no arquivo, a partir de 1.</param>
    /// <returns>O caminho relativo, como a ferramenta o recebe.</returns>
    public static string GravarComLinhaTrocada(string caminhoBase, int numero, string linha)
    {
        string[] linhas = File.ReadAllText(Completo(caminhoBase), Encoding.UTF8).Split("\r\n");
        linhas[numero - 1] = linha;

        string caminho = $"dados/{Guid.NewGuid()}.csv";
        File.WriteAllText(Completo(caminho), string.Join("\r\n", linhas), Encoding.UTF8);
        return caminho;
    }

    /// <summary>O que está gravado no banco, do usuário do servidor.</summary>
    public async Task<Gravado> LerGravadoAsync()
    {
        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(_servidor.Usuario.Id);

        return new Gravado(
            await dbContext.Contas.CountAsync(),
            await dbContext.Importacoes.CountAsync(),
            await dbContext.Transacoes.CountAsync(),
            await dbContext.Despesas.OfType<DespesaParcelada>().CountAsync(),
            await dbContext.Despesas.OfType<DespesaAdiada>().CountAsync());
    }

    private static async Task<Extrato> LerExtratoAsync(LayoutArquivoImportacao layout, string caminho)
    {
        await using FileStream arquivo = File.OpenRead(Completo(caminho));

        return layout == LayoutArquivoImportacao.C6ContaCsv
            ? (await new LeitorExtratoContaC6().LerAsync(arquivo, CancellationToken.None)).Value
            : (await new LeitorExtratoContaMercadoPago().LerAsync(arquivo, (SufixoConta)SufixoContaMercadoPago, CancellationToken.None)).Value;
    }

    private IReadOnlyList<CategoriaDespesa> CategoriasResolvidas(IReadOnlyList<Lancamento> lancamentos)
    {
        IEnumerable<CategoriaNova> novas = AliasesCategoriaPendentes
            .Listar(_servidor.Usuario.Id, categoriasDoUsuario: [], lancamentos)
            .Select(alias => new CategoriaNova((TextoObrigatorio)alias.TextoOriginal, alias));

        return CategoriaDespesaFactory.Resolver(_servidor.Usuario.Id, categoriasDoUsuario: [], novas).Value;
    }

    private async Task CadastrarCartoesAsync(Fatura fatura)
    {
        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(_servidor.Usuario.Id);
        foreach (SufixoCartao sufixo in fatura.Sufixos)
        {
            Cartao cartao = CartaoFactory
                .Criar(_servidor.Usuario.Id, cartoesDoUsuario: [], fatura.Banco, sufixo, _diaVencimento, _diaFechamento)
                .Value;
            await dbContext.Cartoes.AddAsync(cartao);
        }

        await dbContext.SaveChangesAsync();
    }
}

/// <summary>As contagens do que está gravado no banco, do usuário do servidor.</summary>
internal sealed record Gravado(int Contas, int Importacoes, int Transacoes, int DespesasParceladas, int DespesasAdiadas)
{
    public static readonly Gravado Nada = new(Contas: 0, Importacoes: 0, Transacoes: 0, DespesasParceladas: 0, DespesasAdiadas: 0);
}
