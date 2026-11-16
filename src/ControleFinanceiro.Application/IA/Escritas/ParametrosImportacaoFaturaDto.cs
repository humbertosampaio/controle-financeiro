namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// O que o usuário informa na importação de uma fatura, além do arquivo. Cada campo só é lido no layout que o pede, pelo
/// <see cref="ImportacaoFaturaParaModeloService"/>.
/// </summary>
/// <param name="Vencimento">A data de vencimento em <c>yyyy-MM-dd</c>, no layout sem o vencimento no arquivo.</param>
public sealed record ParametrosImportacaoFaturaDto(string? Vencimento);
