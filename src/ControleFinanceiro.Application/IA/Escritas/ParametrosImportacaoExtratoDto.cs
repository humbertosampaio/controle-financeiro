namespace ControleFinanceiro.Application.IA.Escritas;

/// <summary>
/// O que o usuário informa na importação de um extrato, além do arquivo. Cada campo só é lido no layout que o pede, pelo
/// <see cref="ImportacaoExtratoParaModeloService"/>.
/// </summary>
/// <param name="SufixoConta">Os 4 últimos caracteres do número da conta, no layout sem a conta no arquivo.</param>
public sealed record ParametrosImportacaoExtratoDto(string? SufixoConta);
