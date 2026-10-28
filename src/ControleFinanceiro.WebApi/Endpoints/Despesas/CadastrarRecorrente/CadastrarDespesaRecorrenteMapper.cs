using ControleFinanceiro.Domain.Despesas;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Despesas.CadastrarRecorrente;

/// <summary>
/// Só monta a resposta: a recorrente nasce na <see cref="Domain.Services.DespesaRecorrenteFactory"/>, que precisa
/// das recorrentes do usuário.
/// </summary>
/// <remarks>
/// Herda o <see cref="Mapper{TRequest, TResponse, TEntity}"/> completo, sem o <c>ToEntity</c>, porque o
/// <c>SendMapped</c> do endpoint faz cast para ele: um <c>ResponseMapper</c> compila e responde 500.
/// </remarks>
public sealed class CadastrarDespesaRecorrenteMapper
    : Mapper<CadastrarDespesaRecorrenteRequest, CadastrarDespesaRecorrenteResponse, DespesaRecorrente>
{
    public override CadastrarDespesaRecorrenteResponse FromEntity(DespesaRecorrente e) => new(e.Id);
}
