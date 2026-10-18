using ControleFinanceiro.Domain.Shared;
using ControleFinanceiro.Infrastructure.Persistence;
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
    .AddFastEndpoints()
    .SwaggerDocument()
    //.AddApplication()
    .AddPersistence(builder.Configuration);

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
        c.Errors.UseProblemDetails();
    });

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
    app.UseSwaggerGen();

await app.RunAsync();
