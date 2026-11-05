using System.ComponentModel;
using ControleFinanceiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace ControleFinanceiro.Mcp.Ferramentas;

[McpServerToolType]
internal sealed class VerificarServidorTool
{
    [McpServerTool(Name = "verificar_servidor", ReadOnly = true)]
    [Description("Verifica se o servidor do Controle Financeiro está de pé e consegue acessar o banco de dados.")]
    public static async Task<VerificacaoServidor> Verificar(ControleFinanceiroDbContext dbContext, CancellationToken cancellationToken)
    {
        bool acessivel = await dbContext.Database.CanConnectAsync(cancellationToken);
        return new VerificacaoServidor(acessivel ? "disponivel" : "indisponivel");
    }
}

/// <summary>O que vai ao modelo: só o estado do banco, sem host, porta, usuário ou texto de exceção.</summary>
internal sealed record VerificacaoServidor(string Banco);
