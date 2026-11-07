namespace ControleFinanceiro.AppHost.Resources;

internal sealed class ControleFinanceiroDatabase
{
    internal IResourceBuilder<PostgresDatabaseResource> Resource { get; }

    public ControleFinanceiroDatabase(IDistributedApplicationBuilder builder)
    {
        IResourceBuilder<ParameterResource> postgresUser = builder.AddParameter("PostgresUser");
        IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("PostgresPassword", secret: true);

        if (!int.TryParse(builder.Configuration["Postgres:ContainerPort"], out int containerPort))
            throw new InvalidOperationException("Configuração da porta do container do Postgres inválida.");

        if (!int.TryParse(builder.Configuration["Postgres:HostPort"], out int hostPort))
            throw new InvalidOperationException("Configuração da porta do host do Postgres inválida.");

        string? volumeTarget = builder.Configuration["Postgres:VolumeTarget"];
        if (volumeTarget is null)
            throw new InvalidOperationException("Configuração do diretório do volume do Postgres inválida.");

        IResourceBuilder<PostgresServerResource> postgres = builder
            .AddPostgres("postgres")
            .WithImageRegistry(PostgresImagem.Registro)
            .WithImage(PostgresImagem.Imagem)
            .WithImageTag(PostgresImagem.Tag)
            .WithContainerName("database")
            // O Docker Desktop agrupa os containers pelo projeto do Compose.
            .WithContainerRuntimeArgs("--label", "com.docker.compose.project=controle-financeiro", "--label", "com.docker.compose.service=database")
            .WithUserName(postgresUser)
            .WithPassword(postgresPassword)
            .WithEndpoint(
                name: "postgres-endpoint",
                scheme: "tcp",
                port: hostPort,
                targetPort: containerPort,
                isProxied: false)
            .WithVolume("postgres-volume", volumeTarget)
            .WithLifetime(ContainerLifetime.Persistent);

        Resource = postgres.AddDatabase("controlefinanceiro");
    }
}
