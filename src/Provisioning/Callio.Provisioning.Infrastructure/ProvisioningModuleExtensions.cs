using Callio.Provisioning.Application;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
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
        services.AddScoped<ITenantVectorStoreProvisioner, DevelopmentTenantVectorStoreProvisioner>();
        services.AddScoped<ITenantKnowledgeBaseSettingsProvisioner, TenantKnowledgeBaseSettingsProvisioner>();
        services.AddSingleton<ITenantResourceNamingStrategy, DefaultTenantResourceNamingStrategy>();

        services.AddDbContext<ProvisioningDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));

        return services;
    }
}
