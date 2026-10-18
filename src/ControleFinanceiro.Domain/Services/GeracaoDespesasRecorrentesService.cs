using ControleFinanceiro.Domain.Despesas;
using ControleFinanceiro.Domain.Shared.ValueObjects;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;

namespace ControleFinanceiro.Domain.Services;

public static class GeracaoDespesasRecorrentesService
{
    public static Result<IReadOnlyList<Despesa>> GerarParaMes(Usuario usuario, MesReferencia mesReferencia)
    {
        Result<Despesa>[] resultados = usuario.DespesasRecorrentes
            .Select(despesaRecorrente => DespesaFactory.Criar(usuario, despesaRecorrente, mesReferencia))
            .ToArray();

        return Result
            .Combine(resultados)
            .Map<IReadOnlyList<Despesa>>(() => resultados.Select(r => r.Value).ToArray());
    }
}

