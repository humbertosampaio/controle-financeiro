namespace ControleFinanceiro.Domain.Despesas;

/// <summary>O que uma conciliação das parcelas fez.</summary>
/// <param name="Vinculadas">Quantas transações foram ligadas a uma parcela.</param>
/// <param name="SemParcela">Quantas transações candidatas não acharam parcela livre e serão tentadas de novo na próxima chamada.</param>
public sealed record class ConciliacaoParcelas(int Vinculadas, int SemParcela);
