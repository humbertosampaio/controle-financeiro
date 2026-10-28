using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Infrastructure.Importacao;
using ControleFinanceiro.Infrastructure.Persistence;
using ControleFinanceiro.WebApi;
using ControleFinanceiro.WebApi.Autenticacao;
using CSharpFunctionalExtensions;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

Result.Configuration.ErrorMessagesSeparator = "\n";
ValidatorOptions.Global.LanguageManager.Culture = AppDefaults.CulturaPtBr;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services
    //.AddApplication()
    .AddInfrastructurePersistence(builder.Configuration)
    .AddInfrastructureImportacao()
    .AddWebApi();

WebApplication app = builder.Build();


if (app.Environment.IsDevelopment())
{
    await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
    {
        ControleFinanceiroDbContext dbContext = scope.ServiceProvider.GetRequiredService<ControleFinanceiroDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

app.MapDefaultEndpoints()
    .UseFastEndpoints(c =>
    {
        c.Endpoints.RoutePrefix = "api";
        // O Configurator é um delegate só: outra configuração global de endpoint se compõe aqui dentro.
        c.Endpoints.Configurator = UsuarioRotaPreProcessor.Aplicar;
        c.Errors.UseProblemDetails();
    });

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
    app.UseSwaggerGen();

await app.RunAsync();
