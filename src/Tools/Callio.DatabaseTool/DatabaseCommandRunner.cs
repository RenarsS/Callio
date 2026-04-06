using Callio.Admin.Infrastructure.Persistence;
using Callio.Identity.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Callio.DatabaseTool;

internal sealed class DatabaseCommandRunner(
    AdminDbContext adminDbContext,
    AppIdentityDbContext identityDbContext,
    ProvisioningDbContext provisioningDbContext,
    IProvisioningMetadataStoreProvisioner provisioningMetadataStoreProvisioner,
    TenantSchemaMigrationRunner tenantSchemaMigrationRunner,
    TestTenantRequestSeeder testTenantRequestSeeder,
    ILogger<DatabaseCommandRunner> logger)
{
    public async Task<int> RunAsync(DatabaseToolCommand command, CancellationToken cancellationToken)
    {
        switch (command)
        {
            case DatabaseToolCommand.MigrateAll:
                await MigrateMainDatabasesAsync(cancellationToken);
                await MigrateTenantDatabasesAsync(cancellationToken);
                return 0;
            case DatabaseToolCommand.SeedTestData:
                await MigrateMainDatabasesAsync(cancellationToken);
                await testTenantRequestSeeder.SeedAsync(cancellationToken);
                return 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command, "Unsupported database tool command.");
        }
    }

    private async Task MigrateMainDatabasesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying admin schema migrations and baseline reference data.");
        await AdminDataSeeder.SeedAsync(adminDbContext, cancellationToken);

        logger.LogInformation("Applying identity schema migrations.");
        await identityDbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Applying provisioning schema migrations.");
        await provisioningDbContext.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Ensuring provisioning metadata compatibility objects.");
        await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);
    }

    private async Task MigrateTenantDatabasesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying tenant schema migrations.");
        var migratedSchemas = await tenantSchemaMigrationRunner.MigrateAllAsync(cancellationToken);
        logger.LogInformation("Tenant schema migration completed for {SchemaCount} schema(s).", migratedSchemas);
    }
}
