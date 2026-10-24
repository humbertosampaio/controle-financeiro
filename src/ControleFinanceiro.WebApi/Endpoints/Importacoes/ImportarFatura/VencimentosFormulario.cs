using ControleFinanceiro.Domain.Cartoes;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.WebApi.Endpoints.Importacoes.ImportarFatura;

/// <summary>
/// Monta os <see cref="VencimentosCartoesNovos"/> a partir do campo <c>vencimentos</c> do formulário. O validator e o
/// endpoint passam por aqui, para a regra ser a do domínio nos dois.
/// </summary>
internal static class VencimentosFormulario
{
    /// <summary>
    /// Falha com todos os finais ou dias inválidos juntos, ou com o final repetido depois de aparado (<c>" 1234"</c> e
    /// <c>"1234"</c>).
    /// </summary>
    public static Result<VencimentosCartoesNovos> Ler(DiaVencimentoCartao? diaUnico, IEnumerable<KeyValuePair<string, int>>? vencimentos)
    {
        return (vencimentos ?? [])
            .Select(LerVencimento)
            .Combine()
            .Bind(porSufixo => VencimentosCartoesNovos.Criar(diaUnico, porSufixo));
    }

    private static Result<KeyValuePair<SufixoCartao, DiaVencimentoCartao>> LerVencimento(KeyValuePair<string, int> vencimento)
    {
        return SufixoCartao
            .Criar(vencimento.Key)
            .Bind(sufixo => DiaVencimentoCartao
                .Criar(vencimento.Value)
                .Map(dia => KeyValuePair.Create(sufixo, dia)));
    }
}
