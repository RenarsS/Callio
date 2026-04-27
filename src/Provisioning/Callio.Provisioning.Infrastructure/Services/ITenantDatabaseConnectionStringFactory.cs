namespace Callio.Provisioning.Infrastructure.Services;

public interface ITenantDatabaseConnectionStringFactory
{
    string CreateTenantConnectionString();
}
