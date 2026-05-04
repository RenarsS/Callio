using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Callio.Generation.Infrastructure.Persistence;
using Callio.Generation.Infrastructure.Provisioners;
using Callio.Generation.Infrastructure.Repositories;
using Callio.Generation.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Generation.Infrastructure;

public static class GenerationModuleExtensions
{
    public static IServiceCollection AddGenerationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantGenerationOptions>(
            configuration.GetSection(TenantGenerationOptions.SectionName));

        services.AddSingleton<ITenantGenerationDbContextFactory, TenantGenerationDbContextFactory>();
        services.AddScoped<ITenantGenerationStoreProvisioner, SqlServerTenantGenerationStoreProvisioner>();
        services.AddScoped<ITenantGenerationRepository, TenantGenerationRepository>();
        services.AddScoped<IGenerationPromptCatalog, GenerationPromptCatalog>();
        services.AddScoped<IGenerationKnowledgeSourceProvider, TenantGenerationKnowledgeSourceProvider>();
        services.AddScoped<DeterministicGenerationCompletionClient>();
        services.AddScoped<OpenAiGenerationCompletionClient>();
        services.AddScoped<IGenerationCompletionClient, TenantGenerationCompletionClient>();
        services.AddScoped<ITenantGenerationService, TenantGenerationService>();

        return services;
    }
}
