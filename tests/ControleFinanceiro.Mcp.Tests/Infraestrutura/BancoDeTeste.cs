using AutoFixture;
using ControleFinanceiro.Domain.Beneficiarios;
using ControleFinanceiro.Domain.Categorias;
using ControleFinanceiro.Domain.Contas;
using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Importacoes;
using ControleFinanceiro.Domain.Services;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Transacoes;
using ControleFinanceiro.Domain.Usuarios;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.Tests.Shared;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Grava uma despesa recorrente no Pix, com o beneficiário dela e a categoria Outros do usuário, criada na primeira
    /// recorrente dele. O usuário, a descrição, o valor e o dia vêm do <paramref name="fixture"/>.
    /// </summary>
    public async Task<DespesaRecorrente> GravarRecorrenteAsync(IFixture fixture)
    {
        IdUsuario idUsuario = fixture.Create<IdUsuario>();

        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(idUsuario);
        CategoriaDespesa? outros = await dbContext.Categorias.FirstOrDefaultAsync();
        if (outros is null)
        {
            outros = CategoriaDespesaFactory.CriarOutros(idUsuario);
            await dbContext.Categorias.AddAsync(outros);
        }

        TextoObrigatorio descricao = fixture.Create<TextoObrigatorio>();
        Beneficiario beneficiario = BeneficiarioFactory
            .Resolver(idUsuario, beneficiariosDoUsuario: [], [new BeneficiarioNovo(descricao, alias: null)])
            .Value[0];
        DespesaRecorrente recorrente = DespesaRecorrenteFactory
            .CriarSemCartao(
                idUsuario,
                recorrentesDoUsuario: [],
                beneficiario,
                descricao,
                outros,
                FormaPagamento.Pix,
                fixture.Create<ReaisPositivo>(),
                fixture.Create<DiaOcorrenciaRecorrente>())
            .Value;

        await dbContext.Beneficiarios.AddAsync(beneficiario);
        await dbContext.DespesasRecorrentes.AddAsync(recorrente);
        await dbContext.SaveChangesAsync();

        return recorrente;
    }

    /// <summary>Lê a despesa como está no banco, num contexto novo, para conferir o que o servidor gravou.</summary>
    public async Task<Despesa> LerDespesaAsync(Despesa despesa)
    {
        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(despesa.IdUsuario);
        return await dbContext.Despesas.AsNoTracking().SingleAsync(d => d.Id == despesa.Id);
    }

    /// <summary>Grava a despesa que a recorrente gera no mês, pendente, como a geração do mês faria.</summary>
    public async Task<Despesa> GravarDespesaDoMesAsync(DespesaRecorrente recorrente, MesReferencia mes)
    {
        Despesa despesa = GeracaoDespesasDoMesService.GerarComPagamentoImediato(recorrente, mes).Value;

        await using ControleFinanceiroDbContext dbContext = _servidor.CriarDbContext(recorrente.IdUsuario);
        await dbContext.Despesas.AddAsync(despesa);
        await dbContext.SaveChangesAsync();

        return despesa;
    }
}
