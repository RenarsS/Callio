using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Infrastructure.Options;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Knowledge.Infrastructure.Repositories;
using Callio.Knowledge.Infrastructure.Services;
using Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Knowledge.Infrastructure;

public static class KnowledgeModuleExtensions
{
    public static IServiceCollection AddKnowledgeModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantKnowledgeConfigurationOptions>(
            configuration.GetSection(TenantKnowledgeConfigurationOptions.SectionName));
        services.Configure<TenantKnowledgeIngestionOptions>(
            configuration.GetSection(TenantKnowledgeIngestionOptions.SectionName));
        services.Configure<TenantGenerationSourceRetrievalOptions>(
            configuration.GetSection(TenantGenerationSourceRetrievalOptions.SectionName));

        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseSqlServer(
                BuildKnowledgeConnectionString(configuration),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable(
                        SqlServerTransientRetry.MigrationsHistoryTable,
                        SqlServerTransientRetry.MigrationsHistorySchema)
                    .CommandTimeout(60)
                    .EnableRetryOnFailure(
                        SqlServerTransientRetry.MaxRetryCount,
                        SqlServerTransientRetry.MaxRetryDelay,
                        SqlServerTransientRetry.AdditionalErrorNumbers)));

        services.AddScoped<IKnowledgeMetadataStoreProvisioner, SqlServerKnowledgeMetadataStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationStoreProvisioner, SqlServerTenantKnowledgeConfigurationStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeDocumentStoreProvisioner, SqlServerTenantKnowledgeDocumentStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationRepository, TenantKnowledgeConfigurationRepository>();
        services.AddScoped<ITenantKnowledgeDocumentRepository, TenantKnowledgeDocumentRepository>();
        services.AddScoped<ITenantKnowledgeConfigurationService, TenantKnowledgeConfigurationService>();
        services.AddScoped<ITenantKnowledgeConfigurationSetupService, TenantKnowledgeConfigurationSetupService>();
        services.AddScoped<ITenantKnowledgeDocumentService, TenantKnowledgeDocumentService>();
        services.AddScoped<ITenantKnowledgeDashboardService, TenantKnowledgeDashboardService>();
        services.AddScoped<ITenantKnowledgeVectorStore, TenantKnowledgeVectorStore>();
        services.AddScoped<ITenantKnowledgeFileMetadataFactory, TenantKnowledgeFileMetadataFactory>();
        services.AddScoped<ITenantKnowledgeDocumentChunker, TenantKnowledgeDocumentChunker>();
        services.AddScoped<ITenantKnowledgeDocumentProcessor, TenantKnowledgeDocumentProcessor>();
        services.AddSingleton<ITenantKnowledgeConfigurationDbContextFactory, TenantKnowledgeConfigurationDbContextFactory>();
        services.AddSingleton<ITenantKnowledgeDocumentDbContextFactory, TenantKnowledgeDocumentDbContextFactory>();
        services.AddScoped<FileSystemTenantKnowledgeBlobStorage>();
        services.AddScoped<AzureBlobTenantKnowledgeBlobStorage>();
        services.AddScoped<ITenantKnowledgeBlobStorage, TenantKnowledgeBlobStorage>();
        services.AddScoped<DeterministicTenantEmbeddingGenerator>();
        services.AddScoped<OpenAiTenantEmbeddingGenerator>();
        services.AddScoped<ITenantEmbeddingGenerator, TenantEmbeddingGenerator>();
        services.AddScoped<ITenantKnowledgeTextExtractor, TenantKnowledgeTextExtractor>();

        return services;
    }

    private static string BuildKnowledgeConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CallioDb")
            ?? throw new InvalidOperationException("Connection string 'CallioDb' is required for knowledge storage.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.ConnectTimeout = Math.Max(60, builder.ConnectTimeout);
        builder.ConnectRetryCount = 3;
        builder.ConnectRetryInterval = 10;
        builder.MultipleActiveResultSets = true;

        return builder.ConnectionString;
    }
}
