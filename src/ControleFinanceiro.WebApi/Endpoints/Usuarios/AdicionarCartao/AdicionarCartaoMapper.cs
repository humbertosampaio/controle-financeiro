using ControleFinanceiro.Domain.Cartoes;
using FastEndpoints;

namespace ControleFinanceiro.WebApi.Endpoints.Usuarios.AdicionarCartao;

/// <summary>
/// Só monta a resposta: o cartão nasce na <see cref="Domain.Services.CartaoFactory"/>, que precisa dos cartões
/// do usuário.
/// </summary>
/// <remarks>
/// Herda o <see cref="Mapper{TRequest, TResponse, TEntity}"/> completo, sem o <c>ToEntity</c>, porque o
/// <c>SendMapped</c> do endpoint faz cast para ele: um <c>ResponseMapper</c> compila e responde 500.
/// </remarks>
public sealed class AdicionarCartaoMapper : Mapper<AdicionarCartaoRequest, AdicionarCartaoResponse, Cartao>
{
    public override AdicionarCartaoResponse FromEntity(Cartao e) => new(e.Id);
}
