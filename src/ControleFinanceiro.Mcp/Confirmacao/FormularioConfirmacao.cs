using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ControleFinanceiro.Mcp.Confirmacao;

/// <summary>
/// O formulário que o cliente mostra ao usuário, abaixo da mensagem que diz o que vai ser feito. A confirmação é um campo
/// marcado, e não só o "aceitar", para um cliente que trate o aceite de forma frouxa não confirmar sozinho.
/// </summary>
internal sealed class FormularioConfirmacao
{
    [JsonPropertyName("confirmar")]
    [Description("Marque para confirmar o que a mensagem descreve.")]
    public bool Confirmar { get; set; }
}
