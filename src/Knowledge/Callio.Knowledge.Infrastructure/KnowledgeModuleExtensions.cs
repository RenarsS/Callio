using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Infrastructure.Options;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Knowledge.Infrastructure.Repositories;
using Callio.Knowledge.Infrastructure.Services;
using Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;
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

        services.AddDbContext<KnowledgeDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));

        services.AddScoped<IKnowledgeMetadataStoreProvisioner, SqlServerKnowledgeMetadataStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationStoreProvisioner, SqlServerTenantKnowledgeConfigurationStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeDocumentStoreProvisioner, SqlServerTenantKnowledgeDocumentStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationRepository, TenantKnowledgeConfigurationRepository>();
        services.AddScoped<ITenantKnowledgeDocumentRepository, TenantKnowledgeDocumentRepository>();
        services.AddScoped<ITenantKnowledgeConfigurationService, TenantKnowledgeConfigurationService>();
        services.AddScoped<ITenantKnowledgeConfigurationSetupService, TenantKnowledgeConfigurationSetupService>();
        services.AddScoped<ITenantKnowledgeDocumentService, TenantKnowledgeDocumentService>();
        services.AddScoped<ITenantKnowledgeDashboardService, TenantKnowledgeDashboardService>();
        services.AddSingleton<ITenantKnowledgeConfigurationDbContextFactory, TenantKnowledgeConfigurationDbContextFactory>();
        services.AddSingleton<ITenantKnowledgeDocumentDbContextFactory, TenantKnowledgeDocumentDbContextFactory>();
        services.AddScoped<FileSystemTenantKnowledgeBlobStorage>();
        services.AddScoped<AzureBlobTenantKnowledgeBlobStorage>();
        services.AddScoped<ITenantKnowledgeBlobStorage, TenantKnowledgeBlobStorage>();
        services.AddScoped<DeterministicTenantEmbeddingGenerator>();
        services.AddScoped<ITenantEmbeddingGenerator, TenantEmbeddingGenerator>();
        services.AddScoped<ITenantKnowledgeTextExtractor, TenantKnowledgeTextExtractor>();
        services.AddScoped<AzureOpenAiTenantEmbeddingGenerator>();

        return services;
    }
}
