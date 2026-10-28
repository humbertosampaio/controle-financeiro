using ControleFinanceiro.Application.Abstractions;
using ControleFinanceiro.Domain.Usuarios;
using CSharpFunctionalExtensions;
using FastEndpoints;
using Microsoft.AspNetCore.Routing.Patterns;

namespace ControleFinanceiro.WebApi.Autenticacao;

/// <summary>
/// Recusa com 404 a requisição cujo <c>{idUsuario}</c> da rota não é de um usuário cadastrado, antes do handler.
/// É ligado por convenção, em <see cref="Aplicar"/>, a todo endpoint com esse parâmetro na rota.
/// </summary>
public sealed class UsuarioRotaPreProcessor : IGlobalPreProcessor
{
    public static void Aplicar(EndpointDefinition ep)
    {
        bool temIdUsuarioNaRota = ep.Routes.Any(rota => RoutePatternFactory.Parse(rota).GetParameter(UsuarioAtualRota.ParametroRota) is not null);

        if (temIdUsuarioNaRota)
            ep.PreProcessor<UsuarioRotaPreProcessor>(Order.Before);
    }

    public async Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
    {
        // O FastEndpoints também roda os pré-processadores quando a validação falha; aí o 400 vence.
        if (context.HasValidationFailures)
            return;

        // O pré-processador é singleton, e as duas dependências são scoped.
        IUsuarioAtual usuarioAtual = context.HttpContext.Resolve<IUsuarioAtual>();
        IUsuarioRepository usuarioRepository = context.HttpContext.Resolve<IUsuarioRepository>();

        Maybe<Usuario> usuario = await usuarioRepository.ObterPorIdAsync(usuarioAtual.Id, ct);

        if (usuario.HasNoValue)
            await context.HttpContext.Response.SendNotFoundAsync(ct);
    }
}
