using System.Text.Json.Nodes;

namespace ControleFinanceiro.Tests.Shared.Respostas;

public static class JsonLidoExtensions
{
    extension(JsonObject json)
    {
        public string Texto(string campo) => json[campo]!.GetValue<string>();
    }
}
