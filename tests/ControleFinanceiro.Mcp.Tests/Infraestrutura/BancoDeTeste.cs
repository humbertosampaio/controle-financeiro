using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Tests.Shared;

namespace ControleFinanceiro.Mcp.Tests.Infraestrutura;

/// <summary>
/// Grava pelo <c>DbContext</c>, fora do processo do servidor, o que as ferramentas de leitura vão ler. Cada agregado
/// nasce pela factory do domínio, como na aplicação, e é gravado com tudo de que depende pelas FKs.
/// </summary>
internal sealed class BancoDeTeste
{
    private readonly ServidorMcpFixture _servidor;

    public BancoDeTeste(ServidorMcpFixture servidor)
    {
        _servidor = servidor;
    }

    public async Task GravarUsuarioAsync(Usuario usuario)
    {
        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(usuario.Id);
        await dbContext.Usuarios.AddAsync(usuario);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Grava a conta, a importação, os beneficiários das saídas e as transações dos lançamentos, como um extrato
    /// importado. Os lançamentos não podem ter categoria do banco.
    /// </summary>
    /// <param name="importacao">A importação da mesma conta.</param>
    public async Task<IReadOnlyList<Transacao>> GravarExtratoAsync(
        ContaBancaria conta,
        ImportacaoArquivo importacao,
        IReadOnlyList<Lancamento> lancamentos)
    {
        IReadOnlyList<Beneficiario> beneficiarios = BeneficiariosResolvidos.Dos(conta.IdUsuario, lancamentos);
        IReadOnlyList<Transacao> transacoes = LoteTransacoesFactory
            .Criar(conta.IdUsuario, beneficiarios, categoriasDoUsuario: [], conta, importacao, lancamentos)
            .Value;

        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(conta.IdUsuario);
        await dbContext.Contas.AddAsync(conta);
        await dbContext.Importacoes.AddAsync(importacao);
        await dbContext.Beneficiarios.AddRangeAsync(beneficiarios);
        await dbContext.Transacoes.AddRangeAsync(transacoes);
        await dbContext.SaveChangesAsync();

        return transacoes;
    }
}
