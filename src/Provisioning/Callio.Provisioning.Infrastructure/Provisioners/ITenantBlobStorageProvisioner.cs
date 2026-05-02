namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface ITenantBlobStorageProvisioner
{
    Task EnsureCreatedAsync(string containerName, CancellationToken cancellationToken = default);
}
