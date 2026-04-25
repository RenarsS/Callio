namespace Callio.Generation.Infrastructure.Provisioners;

public interface ITenantGenerationStoreProvisioner
{
    Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default);
}
