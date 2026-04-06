using Callio.Provisioning.Application;
using Callio.Provisioning.Application.KnowledgeConfigurations;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Repositories;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Provisioning.Infrastructure;

public static class ProvisioningModuleExtensions
{
    public static IServiceCollection AddProvisioningModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantProvisioningOptions>(configuration.GetSection(TenantProvisioningOptions.SectionName));

        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<ITenantDatabaseSchemaProvisioner, SqlServerTenantDatabaseSchemaProvisioner>();
        services.AddScoped<IProvisioningMetadataStoreProvisioner, SqlServerProvisioningMetadataStoreProvisioner>();
        services.AddScoped<ITenantVectorStoreProvisioner, DevelopmentTenantVectorStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationStoreProvisioner, SqlServerTenantKnowledgeConfigurationStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeConfigurationRepository, TenantKnowledgeConfigurationRepository>();
        services.AddScoped<ITenantKnowledgeConfigurationService, TenantKnowledgeConfigurationService>();
        services.AddScoped<ITenantKnowledgeConfigurationSetupService, TenantKnowledgeConfigurationSetupService>();
        services.AddSingleton<ITenantResourceNamingStrategy, DefaultTenantResourceNamingStrategy>();
        services.AddSingleton<ITenantKnowledgeConfigurationDbContextFactory, TenantKnowledgeConfigurationDbContextFactory>();

        services.AddDbContext<ProvisioningDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));

        return services;
    }
}
