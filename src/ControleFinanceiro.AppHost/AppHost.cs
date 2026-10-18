using ControleFinanceiro.AppHost.Resources;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

ControleFinanceiroDatabase controleFinanceiroDatabase = new(builder);

builder.AddProject<Projects.ControleFinanceiro_WebApi>("controlefinanceiro-webapi")
    .WithReference(controleFinanceiroDatabase.Resource)
    .WaitFor(controleFinanceiroDatabase.Resource);

await builder.Build().RunAsync();
