namespace ControleFinanceiro.Application.IA;

/// <summary>
/// O nome publicado de cada ferramenta do servidor MCP. O valor é contrato: mudá-lo quebra o cliente MCP e as regras
/// <c>ask</c> do <c>.claude/settings.json</c>, que o <c>FerramentasQueGravamTests</c> confere.
/// </summary>
public static class Tools
{
    public const string VerificarServidor = "verificar_servidor";

    public const string ListarTransacoes = "listar_transacoes";
    public const string ListarDespesasFixas = "listar_despesas_fixas";
    public const string BalancoMensal = "balanco_mensal";

    public const string PreverVinculo = "prever_vinculo";
    public const string ConfirmarVinculo = "confirmar_vinculo";

    public const string ListarCartoes = "listar_cartoes";
    public const string ListarCandidatosPagamentoFatura = "listar_candidatos_pagamento_fatura";
    public const string PreverPagamentoFatura = "prever_pagamento_fatura";
    public const string ConfirmarPagamentoFatura = "confirmar_pagamento_fatura";

    public const string PreverImportacao = "prever_importacao";
    public const string ConfirmarImportacao = "confirmar_importacao";
}
