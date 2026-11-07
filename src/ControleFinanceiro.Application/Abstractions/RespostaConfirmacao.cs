namespace ControleFinanceiro.Application.Abstractions;

/// <summary>O que o usuário respondeu a um pedido de confirmação.</summary>
public enum RespostaConfirmacao
{
    Confirmada,

    /// <summary>O usuário recusou ou cancelou, ou não marcou a confirmação.</summary>
    Recusada,

    /// <summary>Não deu para perguntar: o cliente não aceita a pergunta ou ela falhou. Equivale a não confirmar.</summary>
    Indisponivel,
}
