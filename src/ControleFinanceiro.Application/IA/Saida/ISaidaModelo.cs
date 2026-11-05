namespace ControleFinanceiro.Application.IA.Saida;

/// <summary>
/// Marca o que pode sair para o modelo. Quem o implementa só aceita texto como
/// <see cref="Domain.Shared.ValueObjects.DocumentoMascarado"/>, como o teste de arquitetura confere. Os campos que cada
/// tipo expõe são conferidos no teste de payload dele e cobrados pelo <c>revisor-privacidade</c>.
/// </summary>
public interface ISaidaModelo;
