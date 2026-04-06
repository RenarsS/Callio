namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface ITenantDatabaseSchemaProvisioner
{
    Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default);
}
