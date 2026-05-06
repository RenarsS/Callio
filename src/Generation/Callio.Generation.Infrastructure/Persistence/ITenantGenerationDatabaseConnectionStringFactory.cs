namespace Callio.Generation.Infrastructure.Persistence;

public interface ITenantGenerationDatabaseConnectionStringFactory
{
    string CreateTenantConnectionString();
}
