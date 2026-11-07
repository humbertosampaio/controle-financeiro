using System.Reflection;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using DependencyInjectionMcp = ControleFinanceiro.Mcp.DependencyInjection;

namespace ControleFinanceiro.SaidaModelo.Tests.Arquitetura;

/// <summary>
/// Toda ferramenta que grava pede a aprovação do usuário duas vezes: pela meta, que o Claude Code no terminal honra em
/// todos os modos, e pela regra <c>ask</c> do projeto, que é o que faz o app desktop do Claude Code perguntar.
/// </summary>
public sealed class FerramentasQueGravamTests
{
    private const string MetaInteracaoDoUsuario = "anthropic/requiresUserInteraction";

    private static readonly string _raiz = Path.GetDirectoryName(SaidaModeloArquiteturaTests.CaminhoDaSolucao())!;

    [Fact]
    public void FerramentasQueGravam_Deve_PedirAInteracaoDoUsuarioPelaMeta()
    {
        FerramentasQueGravam()
            .Where(ferramenta => !ferramenta.GetCustomAttributes<McpMetaAttribute>()
                .Any(meta => meta.Name == MetaInteracaoDoUsuario && JsonNode.Parse(meta.JsonValue)?.GetValue<bool>() == true))
            .Select(ferramenta => ferramenta.Name)
            .Should().BeEmpty();
    }

    [Fact]
    public void FerramentasQueGravam_Deve_TerRegraAskNoProjeto()
    {
        string[] ask = RegrasDoProjeto("ask");

        RegrasDasFerramentasQueGravam().Should().NotBeEmpty().And.BeSubsetOf(ask);
    }

    /// <summary>O <c>allow</c> do servidor inteiro, ou com curinga, também cobre a ferramenta.</summary>
    [Fact]
    public void FerramentasQueGravam_Deve_NaoTerRegraAllowNoProjeto()
    {
        string[] allow = RegrasDoProjeto("allow");

        RegrasDasFerramentasQueGravam()
            .Where(regra => allow.Any(permitida => Cobre(permitida, regra)))
            .Should().BeEmpty();
    }

    private static IEnumerable<MethodInfo> FerramentasQueGravam()
    {
        return typeof(DependencyInjectionMcp).Assembly
            .GetTypes()
            .SelectMany(tipo => tipo.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(metodo => metodo.GetCustomAttribute<McpServerToolAttribute>() is { ReadOnly: false });
    }

    /// <summary>A regra do Claude Code é <c>mcp__&lt;servidor no .mcp.json&gt;__&lt;ferramenta&gt;</c>.</summary>
    private static IEnumerable<string> RegrasDasFerramentasQueGravam()
    {
        IEnumerable<string> servidores = LerJson(".mcp.json")["mcpServers"]!.AsObject().Select(servidor => servidor.Key);

        return servidores.SelectMany(servidor => FerramentasQueGravam()
            .Select(ferramenta => $"mcp__{servidor}__{ferramenta.GetCustomAttribute<McpServerToolAttribute>()!.Name}"));
    }

    private static string[] RegrasDoProjeto(string tipo)
    {
        JsonArray? regras = LerJson(Path.Combine(".claude", "settings.json"))["permissions"]?[tipo]?.AsArray();

        return regras is null ? [] : regras.Select(regra => regra!.GetValue<string>()).ToArray();
    }

    private static bool Cobre(string permitida, string regra)
    {
        string servidor = regra[..regra.LastIndexOf("__", StringComparison.Ordinal)];
        return permitida == regra || permitida == servidor || permitida == $"{servidor}__*";
    }

    private static JsonObject LerJson(string caminho) => JsonNode.Parse(File.ReadAllText(Path.Combine(_raiz, caminho)))!.AsObject();
}
